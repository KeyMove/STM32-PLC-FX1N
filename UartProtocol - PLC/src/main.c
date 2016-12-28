#include "stm32f10x.h"                  // Device header
#include "uartprotocol.h"
#include "Uart.h"
#include "flash.h"
#include "in.h"
#include "out.h"
#include "encode.h"
#include "sled.h"
#include "pulse.h"
#include "flash.h"
#include "timer.h"
#include "runner.h"
#include "FX1N.h"
u8 databuff[RECV_BUFF_LEN+SEND_BUFF_LEN];

u8 PLCBuff[FX1N_BUFFSIZE];

u8 codecount;

void SystemInit()
{
}

void Stm32_Clock_Init(u8 PLL)
{
	unsigned char temp=0;   
 	RCC->CR|=0x00010000;  //????????HSEON
	while(!(RCC->CR>>17));//????????
	RCC->CFGR=0X00000400; //APB1=DIV2;APB2=DIV1;AHB=DIV1;
	PLL-=2;//??2???
	RCC->CFGR|=PLL<<18;   //??PLL? 2~16
	RCC->CFGR|=1<<16;	  //PLLSRC ON 
	FLASH->ACR|=0x32;	  //FLASH 2?????

	RCC->CR|=0x01000000;  //PLLON
	while(!(RCC->CR>>25));//??PLL??
	RCC->CFGR|=0x00000002;//PLL??????	 
	while(temp!=0x02)     //??PLL??????????
	{   
		temp=RCC->CFGR>>2;
		temp&=0x03;
	}
}	
void JTAG_Set(u8 mode)
{
	u32 temp;
	temp=mode;
	temp<<=25;
	RCC->APB2ENR|=1<<0;     //??????	   
	AFIO->MAPR&=0XF8FFFFFF; //??MAPR?[26:24]
	AFIO->MAPR|=temp;       //??jtag??
} 


void PWMDataSet(UartEvent e)
{
	Pwm pwm;
	e->ReadWord();
	pwm.FirstSpeed=pwm.EndSpeed=e->ReadWord();
	pwm.Frequency=e->ReadDWord();
	pwm.AddTime=pwm.MinusTime=e->ReadWord();
	pwm.Quantity=e->ReadDWord();
	if(e->ReadByte())
	{
		pwm.Quantity=-pwm.Quantity;
	}
	if(e->ReadByte())
	{
		AD_DRVI3(&pwm);
	}
	else{
		AD_DRVA3(&pwm);
	}
}

void PWMStopEvent(UartEvent e)
{
	Pwm_Stop();
}

void getinputdata(UartEvent e)
{
	u8 count;
	u8 offset;
	u16 b;
	u32 v = AllIn_State();
	count = e->ReadByte();
	e->WriteByte(count + INPUTCOUNT);
	if (count != 0)
	{
		e->WriteBuff(e->GetBuff(), count / 8);
		e->Seek(count / 8, 1);
	}
	offset = count & 7;
	if (offset) {
		_16T8H(b) = e->ReadByte();//H=0V L=0
		b >>= offset;//H=0 L=V0

		_16T8H(b) = _32T8LL(v);//H=N L=V0
		b >>= 8 - offset;//H=0N L=NV
		e->WriteByte(_16T8L(b));
		b >>= offset;//H=0 L=N0

		_16T8H(b) = _32T8L(v);//H=N L=V0
		b >>= 8 - offset;//H=0N L=NV
		e->WriteByte(_16T8L(b));
		b >>= offset;//H=0 L=N0

		_16T8H(b) = _32T8H(v);//H=N L=V0
		b >>= 8 - offset;//H=0N L=NV
		e->WriteByte(_16T8L(b));
		b >>= offset;//H=0 L=N0

		_16T8H(b) = _32T8HH(v);//H=N L=V0
		b >>= 8 - offset;//H=0N L=NV
		e->WriteByte(_16T8L(b));
		b >>= offset;//H=0 L=N0
	}
	else {
		e->WriteByte(_32T8LL(v));
		e->WriteByte(_32T8L(v));
		e->WriteByte(_32T8H(v));
		e->WriteByte(_32T8HH(v));
	}
	e->SendAckPacket();
	count = (count + INPUTCOUNT + 7) / 8;
	for (offset = 0; offset < count; offset++)
		InputData[offset] = UartProtocol.SendBuff[offset];
}

void setoutputdata(UartEvent e)
{
	u8 count;
	u16 v=0;
	u16 temp;
	count = e->ReadByte();
	if (count != 0) {
		
		if (count >= OUTPUTCOUNT) {
			count -= OUTPUTCOUNT;
			e->WriteByte(count);
			count = (count + 7) / 8;

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
		}
		else{
			_16T8L(v) = e->ReadByte();
			if(count>8)
				_16T8H(v) = e->ReadByte();
			v &= 0xffff >> (16 - count);
			e->WriteByte(0);
		}		
		Write_HalfWord(0, v);
	}
	else {
		e->WriteByte(0);
	}
	e->SendAckPacket();
}

void AliveStats(UartEvent e)
{
	u8 i;
	u8 count;
	if (e->GetLen() <5)
	{
		e->WriteWord(ID);
		e->WriteByte(OUTPUTCOUNT);
		e->WriteByte(INPUTCOUNT);
		e->WriteByte(PWMCOUNT);
		for (i = 0; i < PWMCOUNT; i++) {
			e->WriteDWord(0);
		}
	}
	else {
		e->WriteWord(e->ReadWord());
		e->WriteByte(e->ReadByte() + OUTPUTCOUNT);
		e->WriteByte(e->ReadByte() + INPUTCOUNT);
		e->WriteByte(count+PWMCOUNT);
		count = e->ReadByte();
		for (i = 0; i < count; i++) {
			e->WriteDWord(e->ReadDWord());
		}
		for (; i < count + PWMCOUNT; i++) {
			e->WriteDWord(0);
		}
	}
	e->SendAckPacket();
	StopCode();
	UartProtocol.RegisterCmd(SetOutputPort, setoutputdata);
	UartProtocol.RegisterCmd(GetInputPort, getinputdata);
}

void encode_callback(u8 mode)
{
	
}

void WriteDataCmd(UartEvent e)
{
	u16 addr;
	u8 len;
	u8 *p;
	if (e->GetLen() < 2) {
		e->RelaySend();
		return;
	}
	if (e->ReadByte()){
		addr=e->ReadWord();
		addr+=3072*codecount;
		Flash.Unlock();
		Flash.Erase((u32)Flash.Data + addr);
		Flash.Lock();
		e->WriteByte(1);
		e->SendAckPacket();
	}
	else {
		addr = e->ReadWord();
		addr += 3072*codecount;
		len = e->ReadByte();
		p=e->GetBuff();
		Flash.WriteData((u32)Flash.Data + addr , p, len);
		e->WriteByte(0);
		e->SendAckPacket();
	}
}

void ReadDataCmd(UartEvent e)
{
	u16 pos;
	u8 i=e->ReadByte();
	CodeRunner.LoadCode(codecount);
	switch(i){
		case 0:StopCode();break;
		case 1:
			UartProtocol.RegisterCmd(SetOutputPort, CodeRunner.SetOutput);
			UartProtocol.RegisterCmd(GetInputPort, CodeRunner.GetInput);
			FX1N.Reset();
			break;
		case 2:
			e->WriteByte(2);
			e->WriteWord(CodeRunner.GetCodeSize(codecount));
			e->SendAckPacket();
			break;
		case 3:
			pos=e->ReadWord();
			e->WriteByte(3);
			e->WriteWord(pos);
			e->WriteByte(32);
			e->WriteBuff(CodeRunner.GetCode(codecount)+pos,32);
			e->SendAckPacket();
			break;
	}
}

void flashtest(void) {
	u16 i;
//	u16 dat;
	for (i = 0; i < 256; i++) {
		if (Flash.Data[i]!=i)
		{
			break;
		}
	}
	//if (i < 256)
	//{
	//	Flash.Unlock();
	//	Flash.Erase((u32)Flash.Data);
	//	FLASH->CR|=FLASH_CR_PG;
	//	for (i = 0; i < 256/2; i++)
	//	{
	//		Flash.WaitBusy();
	//		_16T8H(dat) = i * 2;
	//		_16T8L(dat) = i * 2 + 1;
	//		((__IO u16*)Flash.Data)[i] = dat;
	//	}
	//	CLRBIT(FLASH->CR,FLASH_CR_PG); 
	//	Flash.Lock();
	//}
}

void UpdateInput() {
	static u32 v;
	u8 buff[4];
	u32 nv = AllIn_State();
	if (v != nv) {
		v = nv;
		if (UartProtocol.isLink()) {
			buff[0]=(_32T8LL(v));
			buff[1]=(_32T8L(v));
			buff[2]=(_32T8H(v));
			buff[3]=(_32T8HH(v));
			UartProtocol.SendPacket(GetInputPort, buff, 4);
		}
	}
}


u8 acode=0xff;
void SelectCode(){
	u8 code;
	if(!(AllIn_State()&(BIT17|BIT18|BIT19)))
		code=0;
	else if(AllIn_State()&(BIT17|BIT18|BIT19)){
		code=AllIn_State()>>17;
	}
	if(acode!=code)
	{
		codecount=acode=code;
		CodeRunner.StopCode();
		CodeRunner.LoadCode(acode);
	}
}


void PLCReadIO(u8 count,u8* buff){
	u32 v=AllIn_State();
	buff[0]=_32T8LL(v);
	buff[1]=_32T8L(v);
	buff[2]=_32T8H(v);
}
void PLCWriteIO(u8 count,u8 *buff){
	
	Write_HalfWord(0,(buff[1]<<8)|buff[0]);
}

const u8 testcode[]={
	0x00, 0x24, 0xFA, 0xFF, 0x01, 0x56, 0x00, 0x06, 0x01, 0x80, 0x00, 0x80, 0xFB, 0xFF, 0x00, 0x46, 
	0x01, 0x06, 0x05, 0x80, 0x00, 0x80, 0xFC, 0xFF, 0x00, 0x56, 0x00, 0xC5, 0xCA, 0x01, 0x01, 0x84, 
	0x00, 0x0E, 0x02, 0x80, 0x00, 0x80, 0x00, 0x2E, 0x01, 0xC5, 0x02, 0x24, 0x0C, 0x00, 0x00, 0x8E, 
	0xCA, 0x01, 0x03, 0x84, 0x00, 0xD8, 0x00, 0x28, 0xFA, 0xFF, 0x03, 0x56, 0x02, 0x06, 0x05, 0x80, 
	0x00, 0x80, 0xFB, 0xFF, 0x02, 0x46, 0x03, 0x06, 0x05, 0x80, 0x00, 0x80, 0xFC, 0xFF, 0x02, 0x46, 
	0x02, 0xC5, 0x04, 0x24, 0x00, 0xE8, 0x0F, 0x00
};

int main(void)
{
	RCC->APB2ENR |= RCC_APB2ENR_IOPAEN | RCC_APB2ENR_IOPBEN | RCC_APB2ENR_IOPCEN;

	JTAG_Set(1);
	Stm32_Clock_Init(9);
	UART.Init(72, 115200, OnRecvData);
	UART.SendByte(0);
	Timer.Init(72);
	Out_Init();
	In_Init();

	TIM6_Init(100 - 1, 720 - 1);
	TIM4_Pulse();

	SLED.Init();
	encode.Init(encode_callback);
	SLED.Timer1MS(encode.TimerCheck);

	UartProtocol.Init(databuff);
	UartProtocol.AutoAck(ENABLE);

	UartProtocol.RegisterCmd(Alive, AliveStats);
	/*UartProtocol.RegisterCmd(SetOutputPort, setoutputdata);
	UartProtocol.RegisterCmd(GetInputPort, getinputdata);*/
	UartProtocol.RegisterCmd(SetPWMData, PWMDataSet);
	UartProtocol.RegisterCmd(PWMStop, PWMStopEvent);
	UartProtocol.RegisterCmd(WriteData, WriteDataCmd);
	UartProtocol.RegisterCmd(LoadCodeData, ReadDataCmd);

	Timer.Start(0, UartProtocol.Check);
	Timer.Start(1, TimeOutTick);
	Timer.Start(100,SelectCode);
	
	FX1N.Init(PLCBuff);
	FX1N.SetIOCallBack(PLCReadIO,PLCWriteIO);
	//Timer.Start(0,FX1N.CodeLoop);
	Timer.Start(100,FX1N.UpdateTimer);
	//FX1N.LoadCode((u8*)testcode);
	//Timer.Start(0, UpdateInput);
	//flashtest();

	//UartProtocol.RegisterCmd(SetOutputPort, CodeRunner.SetOutput);
	//UartProtocol.RegisterCmd(GetInputPort, CodeRunner.GetInput);
	
	
	
	
		
	
	if(CodeRunner.LoadCode(codecount)){
		UartProtocol.RegisterCmd(SetOutputPort, CodeRunner.SetOutput);
		UartProtocol.RegisterCmd(GetInputPort, CodeRunner.GetInput);
	}
	else{
		UartProtocol.RegisterCmd(SetOutputPort, setoutputdata);
		UartProtocol.RegisterCmd(GetInputPort, getinputdata);
	}
	//LoadCode();

	while (1)
	{
		Timer.Run();
	}
}


