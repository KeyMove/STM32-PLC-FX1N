#include "runner.h"
#include "Flash.h"
#include "bytestream.h"
#include "pulse.h"
#include "out.h"
#include "in.h"
#include "timer.h"
#include "sled.h"
#include "uartprotocol.h"
#include "FX1N.h"

u16 Count;
u16 StartAddr;
u16 PC;
u16 DelayTime;
u8 PCoffset;

u8 TID=0;
u8 KID = 0;

u8 InputLen;
u8* InputSelect;
u8* InputDatabit;
u8 InputFlag;

u8 OutputFlag;
u8 OutputLen;

u8 InputData[32];
u8 OutputData[32];

u8 PWMCount = PWMCOUNT;

u8 StatsBit=0;
u8 StopBit=0xff;
u8 ResetBit=0xff;
u8 StupBit=0xff;
u8 StartBit = 0xff;

u8 CodeIndex=0;

u8 *CodeData;

u8 Status;
u16 CodeSize;
u8 lastCheckIO;
#define public typedef
public enum
{
	RESTOUT = 0,
	OUTSIG = 1,
	INPUTSIG = 2,
	DELAY = 3,
	PWMCONFIG = 4,
	PWMOUT = 5,
	PWMSTOP = 6,
	PLCCODE=7,
	CHECKEUQ=8,
	CHECKNEQ=9,
	SETDATA=10,
	JMPPOS=11,
	
}CodeBinData;

void RunControl(void) {
	static u8 waitup;
//	u8 addr;
//	u8 i;
	u32 io = AllIn_State();
	
	if (waitup) {
		if (io&(1 << StopBit) )return;
		if (io&(1 << StartBit))return;
		if (io&(1 << ResetBit))return;
		if (io&(1 << StupBit))return;
		waitup = 0;
	}

	switch (StatsBit)
	{
	case 0:
		if (StopBit != 0xff)
			if (io & (1 << StopBit)) {
				waitup = 1;
				InputFlag = 0;
				StatsBit = 1;
			}
		break;
	case 1:
		if(StartBit!=0xff)
			if (io&(1 << StartBit)) {
				waitup = 1;
				StatsBit = 0;
			}
		if (ResetBit != 0xff)
			if (io&(1 << ResetBit)) {
				waitup = 1;
				StatsBit = 2;
			}
		if(StupBit !=0xff)
			if (io&(1 << StupBit)) {
				waitup = 1;
				StatsBit = 3;
			}
		break;
	default:
		break;
	}
	if (waitup)
		Timer.Tick.add(10);
}

void SetCodeIndex(u8 select){
	CodeIndex=select;
}

u8 LoadCode(u8 index)
{
	CodeData=(u8*)&Flash.Data[3072*index];
	if (TID != 0)
		return 0;
	PC=0;
	StartAddr=0;
	InputFlag=0;
	
	_16T8H(Count)=CodeData[0];
	_16T8L(Count)=CodeData[1];
	if(Count==0xffff||Count==0)
		return 0;
	PCoffset = 3 + CodeData[2];
	StartAddr = 3 + CodeData[2] + Count * 2;
	ByteStream.SetReadBuff((u8*)&CodeData[3], CodeData[2]);
	do{
		switch (ByteStream.ReadByte()) {
		case 0:
			StopBit = ByteStream.ReadByte();
			break; 
		case 1:
			StartBit = ByteStream.ReadByte();
			break;
		case 2:
			StupBit = ByteStream.ReadByte();
			break;
		case 3:
			ResetBit = ByteStream.ReadByte();
			break;
		case 4:
			CodeSize=ByteStream.ReadWord();
			break;
		default:
			ByteStream.ReadByte();
			break;
		}
	}while (!ByteStream.isReadEmpty());
	if(CodeSize==0){
		CodeSize=CodeRunner.GetCodeSize(index);
	}
	DelayTime=0;
	TID=Timer.Start(0,RunCode);
	if (KID == 0)
		KID = Timer.Start(0, RunControl);
	return 1;
}

void StopCode(void)
{
	if(TID!=0)
		Timer.Stop(TID);
	TID=0;
	if (KID != 0)
		Timer.Stop(KID);
	KID = 0;
}

static u8* getCode(u8 index)
{
	return (u8*)&Flash.Data[3072*index];
}
static u16 getCodeSize(u8 index)
{
	u16 len;
	u16 pos;
	u16 count;
	u8* p=(u8*)&Flash.Data[3072*index];
	_16T8H(len)=p[0];
	_16T8L(len)=p[1];
	if(len==0||len==0xffff)return 0;
	len--;
	len*=2;
	len+=p[2]+3;
	_16T8H(pos)=p[len];
	_16T8L(pos)=p[len+1];	
	pos+=len+2;
	pos/=32;
	pos++;
	len = p[2]; 
	count=3;
	while (len--) {
		if(p[count++]==4)
		{
			_16T8H(pos)=p[count++];
			_16T8L(pos)=p[count++];
			pos/=32;
			pos++;
			break;
		}
		else
		{
			len--;
			count++;
		}
	}
	return pos*32;
}

static void getinputdata(UartEvent e)
{
	u8 count;
	u8 i;
	u8 t;
	count = (e->ReadByte() + 7) / 8;
	if(count==0)return;
	//count = (InputLen + 7) / 8;
	for (i = 0; i < count; i++)
	{
		t=e->ReadByte();
		if ((t & InputSelect[i]) != (InputDatabit[i]& InputSelect[i]))
		{
			count=((t & InputSelect[i])^(InputDatabit[i] & InputSelect[i]));
			lastCheckIO=i*8;
			for(i=0;i<8;i++)
			{
				if(count&1)
				{
					lastCheckIO+=i+1;
					break;
				}
				count>>=1;
			}
			return;		
		}
	}
	lastCheckIO=0xff;
	InputFlag = 0;
	Timer.Target.ready(TID);
}

static void setoutputdata(UartEvent e)
{
	u8 count;
//	u16 v = 0;
//	u16 temp;
	count = e->ReadByte();
	if (count == 0)
	{
		OutputFlag = 0;
		Timer.Target.ready(TID);
	}
}

void InputIOStats(ByteStreamBase *e)
{
	u8 i;
	u8 count;
	u8 buff[4];
	u32 v = AllIn_State();
	InputLen = e->ReadByte();
	InputDatabit = e->GetReadBuff(); 
	e->ReadSeek(1, (InputLen + 7) / 8);
	InputSelect = e->GetReadBuff();
	InputFlag = 1;
	if (InputLen > INPUTCOUNT)
	{
		e->SetWriteBuff(InputData, 32);
		e->WriteByte(INPUTCOUNT);
		e->WriteByte(_32T8LL(v));
		e->WriteByte(_32T8L(v));
		e->WriteByte(_32T8H(v));
		e->WriteByte(_32T8HH(v));
		//InputLen = (INPUTCOUNT+7)/8;
		UartProtocol.SendPacket(GetInputPort, InputData, 5);
		Timer.Tick.add(20);
		InputFlag = 1;
	}
	else {
		buff[0] = (_32T8LL(v));
		buff[1] = (_32T8L(v));
		buff[2] = (_32T8H(v));
		buff[3] = (_32T8HH(v));
		count = (InputLen + 7) / 8;
		for (i = 0; i < count; i++)
			if ((buff[i] & InputSelect[i]) != (InputDatabit[i] & InputSelect[i]))
			{
				count=((buff[i] & InputSelect[i])^(InputDatabit[i] & InputSelect[i]));
				lastCheckIO=i*8;
				for(i=0;i<8;i++)
				{
					if(count&1)
					{
						lastCheckIO+=i+1;
						break;
					}
					count>>=1;
				}
				return;
			}
		lastCheckIO=0xff;
		InputFlag = 0;
	}
}
void Check() {
	u8 i;
	u8 count;
	u8 buff[4];
	u32 v= AllIn_State();
	if (InputLen>INPUTCOUNT) {
		ByteStream.SetWriteBuff(InputData, 32);
		ByteStream.WriteByte(INPUTCOUNT);
		ByteStream.WriteByte(_32T8LL(v));
		ByteStream.WriteByte(_32T8L(v));
		ByteStream.WriteByte(_32T8H(v));
		ByteStream.WriteByte(_32T8HH(v));
		UartProtocol.SendPacket(GetInputPort, InputData, 5);
		Timer.Tick.add(20);
	}
	else {
		//v = AllIn_State();
		buff[0] = (_32T8LL(v));
		buff[1] = (_32T8L(v));
		buff[2] = (_32T8H(v));
		buff[3] = (_32T8HH(v));
		count = (InputLen + 7) / 8;
		for (i = 0; i < count; i++)
			if ((buff[i] & InputSelect[i]) != (InputDatabit[i] & InputSelect[i]))
				return;
		InputFlag = 0;
	}
}

//void RunCode(void) {
//	u16 addr;
//	ByteStreamBase *e = (ByteStreamBase*)&ByteStream;
//	u8 mode;
//	if (InputFlag) {
//
//	}
//}
//static void InputIOStats(ByteStreamBase *e) {
//	u32 v = AllIn_State();
//	e->SetWriteBuff(InputData, 32);
//	InputLen = e->ReadByte();
//	InputSelect = e->GetReadBuff(); 
//	e->ReadSeek(1, (InputLen + 7) / 8);
//	InputEnable = e->GetReadBuff();
//	e->WriteByte(INPUTCOUNT);
//	e->WriteByte(_32T8LL(v));
//	e->WriteByte(_32T8L(v));
//	e->WriteByte(_32T8H(v));
//	e->WriteByte(_32T8HH(v));
//	UartProtocol.SendPacket(GetInputPort, InputData, 5);
//	InputFlag = 1;
//}

static void OutputIOStats(ByteStreamBase *e) {
	u8 count;
	u16 v = 0;
	u16 temp;
	//u8 len;
	e->SetWriteBuff(OutputData, 32);
	count = e->ReadByte();
	if (count != 0) {

		if (count > OUTPUTCOUNT) {
			count -= OUTPUTCOUNT;
			e->WriteByte(count);
			OutputLen = count = (count + 7) / 8;
			OutputLen++;
			_16T8L(v) = e->ReadByte();             //
			temp = _16T8H(v) = e->ReadByte();      //H=0 L=V
			v &= 0x0fff;
			//temp >>= (OUTPUTCOUNT & 7); 
			while (count--) {
				if(count!=0)
					_16T8H(temp) = e->ReadByte();      //H=N L=V
				temp >>= (OUTPUTCOUNT & 7);        //H=0N L=NV
				e->WriteByte(_16T8L(temp));
				temp >>= 8 - (OUTPUTCOUNT & 7);    //H=0 L=N0
			}
			UartProtocol.SendPacket(SetOutputPort, OutputData, OutputLen);
			Timer.Tick.add(20);
			OutputFlag = 1;
		}
		else {
			_16T8L(v) = e->ReadByte();
			if (count > 8)
				_16T8H(v) = e->ReadByte();
			v &= 0xffff >> (16 - count);
			//e->WriteByte(0);
		}
		Write_HalfWord(0, v);
	}
}

void PWMConfig(ByteStreamBase *e) {

}

void PWMSetup(ByteStreamBase *e) {
	u8 count=e->ReadByte();
	u8 i;
	for (i = 0; i < count; i++) {
	}
}

void PWMStopRun(ByteStreamBase *e) {
	
}

void RunCode(void) {
	u16 i;
	u16 addr;
	ByteStreamBase *e = (ByteStreamBase*)&ByteStream;
	u8 mode;
	//if (StatsBit)return;
	if (InputFlag) {
		Check();
		if(lastCheckIO!=255)
			SLED.SetValue(lastCheckIO);
		else
			SLED.SetHex(0xff);
		return;
	}
	if (OutputFlag) {
		UartProtocol.SendPacket(SetOutputPort, OutputData, OutputLen);
		Timer.Tick.add(20);
		return;
	}
	switch (StatsBit) {
	case 0:
		_16T8H(addr) = CodeData[PC * 2 + PCoffset];
		_16T8L(addr) = CodeData[PC * 2 + PCoffset + 1];
		addr += StartAddr;
		mode = CodeData[addr];
		break;
	case 1:return;
	case 2:
		StatsBit = 1;
		if (PC == 0)return;
		while (PC != 0) {
			PC--;
			_16T8H(addr) = CodeData[PC * 2 + PCoffset];
			_16T8L(addr) = CodeData[PC * 2 + PCoffset + 1];
			addr += StartAddr;
			mode = CodeData[addr];
			if (mode == OUTSIG) {
				break;
			}
			if (PC == 0)
				return;
		}
		break;
	case 3:
		StatsBit = 1;
		for (i = 0; i < Count;i++){
			PC++;
			if (PC >= Count) {
				PC = 0;
			}
			_16T8H(addr) = CodeData[PC * 2 + PCoffset];
			_16T8L(addr) = CodeData[PC * 2 + PCoffset + 1];
			addr += StartAddr;
			mode = CodeData[addr];
			if (mode == OUTSIG) {
				break;
			}
		}
		break;
	}
	
	e->SetReadBuff((u8*)&CodeData[addr + 1], 100);
	switch (mode)
	{
	case OUTSIG:
		OutputIOStats(e);
		break;
	case INPUTSIG:
		InputIOStats(e);
		break;
	case DELAY:
		DelayTime = e->ReadWord();
		Timer.Tick.add(DelayTime);
		break;
	case PWMCONFIG:
		PWMConfig(e);
		break;
	case PWMOUT:
		PWMSetup(e);
		break;
	case PWMSTOP:
		PWMStopRun(e);
		break;
	case PLCCODE:
		e->ReadWord();
		FX1N.LoadCode(e->GetReadBuff());
		FX1N.CodeLoop();
		break;
	case CHECKEUQ:InputIOStats(e);Status=InputFlag==0; InputFlag=0; break;
	case CHECKNEQ:InputIOStats(e);Status=!InputFlag==0; InputFlag=0; break;
	case SETDATA:break;
	case JMPPOS:if(Status)PC=e->ReadWord(); break;
	default:
		break;
	}
	//SLED.SetValue(PC);
	if(StatsBit==0)
	{
		if (PC < Count) {
			PC++;
		}
		else {
			PC = 0;
		}
	}
}


//void RunCode(void)
//{
//	u16 addr;
//	ByteStreamBase *e=(ByteStreamBase*)&ByteStream;
//	Pwm pwm;
//	u8 mode;
//	if(InputFlag)
//	{
//		checkInput();
//		return;
//	}
//	_16T8H(addr)=Flash.Data[PC*2+2];
//	_16T8L(addr)=Flash.Data[PC*2+3];
//	addr+=StartAddr;
//	mode=Flash.Data[addr];
//	e->SetReadBuff((u8*)&Flash.Data[addr+1],100);
//	switch(mode)
//	{
//		case RESTOUT:
//			Write_HalfWord(0,0);
//			break;
//		case OUTSIG:
//			_16T8L(addr)=e->ReadByte();
//			_16T8H(addr)=e->ReadByte();
//			Write_HalfWord(0,addr);
//			break;
//		case INPUTSIG:
//			_32T8LL(InputEnable)=e->ReadByte();
//			_32T8L(InputEnable)=e->ReadByte();
//			_32T8H(InputEnable)=e->ReadByte();
//		  _32T8LL(InputSelect)=e->ReadByte();
//			_32T8L(InputSelect)=e->ReadByte();
//			_32T8H(InputSelect)=e->ReadByte();
//			InputEnable&=InputSelect;
//			InputFlag=1;
//			break;
//		case DELAY:
//			DelayTime=e->ReadWord();
//			Timer.Tick.add(DelayTime);
//			break;
//		case PWMOUT:
//			e->ReadWord();
//			pwm.FirstSpeed=pwm.EndSpeed=e->ReadWord();
//			pwm.Frequency=e->ReadDWord();
//			pwm.AddTime=pwm.MinusTime=e->ReadWord();
//			pwm.Quantity=e->ReadDWord();
//			if(e->ReadByte())
//			{
//				pwm.Quantity=-pwm.Quantity;
//			}
//			if(e->ReadByte())
//			{
//				AD_DRVI3(&pwm);
//			}
//			else{
//				AD_DRVA3(&pwm);
//			}
//			break;
//		case PWMSTOP:
//			Pwm_Stop();
//			break;
//	}
//	SLED.SetValue(PC);
//	if(PC<Count)
//		PC++;
//	else
//		PC=0;
//}


const RunnerBase CodeRunner = {
	setoutputdata,
	getinputdata,
	LoadCode,
	RunCode,
	StopCode,
	getCode,
	getCodeSize,
};

