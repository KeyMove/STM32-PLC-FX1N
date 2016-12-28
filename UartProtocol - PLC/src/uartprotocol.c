#include"uartprotocol.h"
#include"uart.h"

//u8 databuff[RECV_BUFF_LEN+SEND_BUFF_LEN];//定义收发缓冲区

UartCallBack UartCmdEvent[MAX_CMD];//命令回调




#ifndef UARTENABLE

typedef struct{
	void(*SendByte)(u8);
}UARTBase;

static void send(u8 dat)
{

}
	

const UARTBase UART={
	send,
};

#endif


u8 recvmode=0;//
u8 RecvFlag=0;//接收标志位
u8 RecvCheck=0;//接收校验
u8 TimeOut=0;
u16 LinkTimeOut = 0;
u8 recvcmd=0;//接收到的串口命令
u8 savecmd;//上一次发送的命令

u16 sendSize=SEND_BUFF_LEN;
u16 sendCount = 0;
u16 sendPos = 0;
u8 *sendbuff;//发送区缓存
u8 *outputbuff;
u16 lastSendCount=0;
u8 sendSum = 0;;

u16 recvPos;
u8 *packbuff;//接收区缓存
u16 packlen=0;//包长度

static const UARTDATA UD;

u8 AutoACK;//接收数据流

void OnRecvData(u8 dat)
{
	static u16 count;
	TimeOut=PacketTime;
	switch(recvmode)
	{
		case 0:
			if(dat==0xAA)
				recvmode=1;
			break;
		case 1:
			packlen=dat;
			count=0;
			RecvCheck=0;
			recvmode=5;
			break;
		case 2:
			packbuff[count]=dat;
			RecvCheck+=dat;
			count++;
			if(packlen<=count){
				recvmode=3;
			}
			break;
		case 3:
			if(dat!=RecvCheck){
				recvmode=0;
				RecvFlag=RF_ERROR;
			}
			else{
				recvmode=4;
			}
			break;
		case 4:
			recvmode=0;
				RecvFlag=RF_OK;
			break;
		case 5:
			packlen<<=8;
			packlen|=dat;
			if(packlen>RECV_BUFF_LEN+SEND_BUFF_LEN)
			{
				recvmode=0;
				break;
			}
			recvmode=6;
			break;
		case 6:
			recvcmd=dat;
			recvmode=2;
			break;
	}
}

void TimeOutTick(void) {
	if(TimeOut)
		if (--TimeOut == 0) {
			recvmode = 0;
		}
	if (LinkTimeOut)
		LinkTimeOut--;
}
//-----------------------------------------------------------------------------

static void Setoffset(s16 offset,u8 flag)
{
	if(flag){
		recvPos+=offset;
	}
	else{
		recvPos=offset;
	}
}

static u8 ReadByte(){
	if((recvPos+1)>packlen){
		return 0;
	}
	return packbuff[recvPos++];
}

static u8 WriteByte(u8 dat) {
	if (sendSize<3) {
		return 0;
	}
	sendSize--;
	sendSum += sendbuff[sendPos++] = dat; if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	sendCount++;
	return 1;
}

static u16 ReadWord() {
	u16 v;
	if ((recvPos + 2) > packlen) {
		return 0;
	}
	_16T8H(v) = packbuff[recvPos++];
	_16T8L(v) = packbuff[recvPos++];
	return v;
}

static u8 WriteWord(u16 dat) {
	if (sendSize < 4) {
		return 0;
	}
	sendSize -= 2;
	sendCount += 2;
	sendSum += sendbuff[sendPos++] = _16T8H(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	sendSum += sendbuff[sendPos++] = _16T8L(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	return 1;
}

static u32 ReadDWord(){
	u32 v;
	if((recvPos+4)>packlen){
		return 0;
	}
	_32T8HH(v)=packbuff[recvPos++];
	_32T8H(v)=packbuff[recvPos++];
	_32T8L(v)=packbuff[recvPos++];
	_32T8LL(v)=packbuff[recvPos++];
	return v;
}

static u8 WriteDWord(u32 dat) {
	if (sendSize < 6) {
		return 0;
	}
	sendSize -= 4;
	sendCount += 4;
	sendSum += sendbuff[sendPos++] = _32T8HH(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	sendSum += sendbuff[sendPos++] = _32T8H(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	sendSum += sendbuff[sendPos++] = _32T8L(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	sendSum += sendbuff[sendPos++] = _32T8LL(dat); if (sendPos == SEND_BUFF_LEN)sendPos = 0;
	return 1;
}

static u16 ReadBuff(u8 *buff,u16 len)
{
	if((len+recvPos)>=packlen){
		len=packlen-recvPos;
	}
	while(len--){
		*buff++=packbuff[recvPos++];
	}
	return len;
}

static u16 WriteBuff(u8 *buff,u16 len)
{
	if(sendSize<(len+2)){
		len = sendSize-2;
	}
	sendSize -= len;
	sendCount += len;
	while(len--){
		sendSum += *buff;
		sendbuff[sendPos++]=*buff++;
	}
	return len;
}

static u16 Writestr(u8 *str)
{
	u16 len=0;
	while((*str!=0)&&(sendSize))
	{
		sendSum += *str;
		sendbuff[sendPos++]=*str++; if (sendPos == SEND_BUFF_LEN)sendPos = 0;
		sendSize --;
		sendCount ++;
		len++;
	}
	return len;
}

static u8* getbuff()
{
	return &packbuff[recvPos];
}

static u16 getlen(){
	return packlen;
};

static u8 getcmd(){
	return recvcmd;
}

static u8* getsendbuff() {
	return &sendbuff[sendPos];
}
//---------------------------------------------------------------------------------

static void Init(u8* databuff)
{
	u16 i;
	for (i = 0; i < MAX_CMD; i++)
	{
		UartCmdEvent[i] = 0;
	}
	for (i = 0; i < RECV_BUFF_LEN + SEND_BUFF_LEN; i++) {
		databuff[i] = 0;
	}
	packbuff = databuff;

	sendbuff = &databuff[RECV_BUFF_LEN];
	outputbuff = sendbuff+SEND_BUFF_LEN/2;
	sendCount = 2;	
	WriteDWord(0xAA000000);
	sendSum = 0;
}

void SendPack(u8 cmd,u8 *buff,u16 len)
{
	u8 ck=0;
	u8 val;
	UART.SendByte(0xAA);
	UART.SendByte(_16T8H(len));
	UART.SendByte(_16T8L(len));
	UART.SendByte(cmd);
	while(len--)
	{
		val=*buff;
		UART.SendByte(val);
		ck+=val;
		buff++;
	}
	UART.SendByte(ck);
	UART.SendByte(0x55);
}

void SendCmdPack(u8 cmd)
{
	UART.SendByte(0xAA);
	UART.SendByte(0);
	UART.SendByte(1);
	UART.SendByte(cmd);
	UART.SendByte(0);
	UART.SendByte(0);
	UART.SendByte(0x55);
}

void RegisterCmdEvent(u8 cmd,UartCallBack function)
{
	UartCmdEvent[cmd]=function;
}

void UnRegisterCmdEvent(UartCallBack function)
{
	u8 i;
	for(i=0;i<MAX_CMD;i++)
	{
		if(UartCmdEvent[i]==function)
		{
			UartCmdEvent[i]=0;
			return;
		}
	}
}

void aack(u8 stats)
{
	AutoACK=stats;
}

void ackpack(u8 cmd)
{
	//SendPack(recvcmd,buff,len);
	u8* pdata = sendbuff;
	u16 packlen=sendCount-6;
	*pdata++ = 0xAA;
	if(packlen==0)
	{
		packlen++;
		WriteByte(0);
	}
	*pdata++ = _16T8H(packlen);
	*pdata++ = _16T8L(packlen);
	*pdata++ = cmd;
	//while(packlen--)
	//	ck+=*pdata++;
	//if(ck!=sendSum)
	//	sendSum=ck;
	WriteByte(sendSum);
	WriteByte(0x55);
	UART.SendBuff(sendbuff, sendCount-2);
	pdata=sendbuff;
	sendbuff=outputbuff;
	outputbuff=pdata;
	sendPos=0;
	sendSize=SEND_BUFF_LEN/2;
	sendCount = 2;
	WriteDWord(0xAA000000);
	sendSum = 0;
	AutoACK+=2;
}

void sendackpacket(void)
{
	if(sendCount<=6)
	{
		WriteByte(0);
	}
	ackpack(recvcmd);
}

void UartCmd(void)
{	
	if(RecvFlag==RF_OK){
		RecvFlag=0;
		recvPos=0;
		//sendPos=0;
		LinkTimeOut = LinkTime;
		if(UartCmdEvent[recvcmd]!=0)
		{
			UartCmdEvent[recvcmd]((UartEvent)&UD);
			if(AutoACK==1){
				ackpack(recvcmd);// SendCmdPack(recvcmd);
			}
			else{
				AutoACK&=1;
			}
		}
	}
	savecmd=recvcmd;
}

u8 isLink(void) {
	return LinkTimeOut != 0;
}

void relaysend(){
	SendPack(recvcmd, packbuff, packlen);
	AutoACK += 2;
}

//--------------------------------------------------------------------

static const UARTDATA UD={
	Setoffset,
	ReadByte,
	ReadWord,
	ReadDWord,
	ReadBuff,
	getbuff,
	getlen,
	getcmd,
	WriteByte,
	WriteWord,
	WriteDWord,
	WriteBuff,
	Writestr,
	getsendbuff,
	sendackpacket,
	relaysend,
};

const UartProtocolBase UartProtocol={
	Init,
	RegisterCmdEvent,
	UnRegisterCmdEvent,
	aack,
	SendCmdPack,
//	ackpack,
	SendPack,
	UartCmd,
	isLink,
	(u8*)&packbuff,
	(u8*)&sendbuff,
};


