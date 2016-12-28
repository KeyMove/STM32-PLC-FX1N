#include"bytestream.h"

static u8 *writebuff;
static u16 writelen;
static u16 writepos;

static u8 *readbuff;
static u16 readlen;
static u16 readpos;


static void setreadbuff(u8* buff,u16 len)
{
	readbuff=buff;
	readlen=len;
	readpos=0;
}

static u8 readempty()
{
	return readpos>=readlen;
}

static void readseek(u8 type, s16 seek) {
	
	u16 len;
	if(type)
		len = readpos + seek;
	else
		len = seek;
	if (len < readlen)
		readpos = len;
}

static u8* getreadbuff(void)
{
	return &readbuff[readpos];
}

static u8 readbyte(void){
	if(readlen!=readpos)
		return readbuff[readpos++];
	return 0;
}

static u16 readword(void){
	u16 dat;
	if((readlen-2)>=readpos){
		_16T8H(dat)=readbuff[readpos++];
		_16T8L(dat)=readbuff[readpos++];
		return dat;
	}
	return 0;
}

static u32 readdword(void){
	u32 dat;
	if((readlen-4)>=readpos){
		_32T8HH(dat)=readbuff[readpos++];
		_32T8H(dat)=readbuff[readpos++];
		_32T8L(dat)=readbuff[readpos++];
		_32T8LL(dat)=readbuff[readpos++];
		return dat;
	}
	return 0;
}

static u16 readbuffdata(u8* buff,u16 len)
{
	u16 v=len;
	while(len--)
	{
		*buff++=readbuff[readpos++];
		if(readlen==readpos)
			break;
	}
	return v-len;
}

static u16 readstring(u8* buff){
	u16 v=readpos;
	u8 dat;
	while(readpos!=readlen)
	{
		dat=readbuff[readpos++];
		if(dat)
			*buff++=dat;
		else
			break;
	}
	return readpos-v;
}





static void setwritebuff(u8* buff,u16 len)
{
	writebuff=buff;
	writelen=len;
	writepos=0;
}

static u8* getwritebuff(void)
{
	return &writebuff[writepos];
}

static void writebyte(u8 dat){
	if(writelen!=writepos)
		writebuff[writepos++]=dat;
}

static void writeword(u16 dat){
	if((writelen-1)>writepos){
		writebuff[writepos++]=_16T8H(dat);
		writebuff[writepos++]=_16T8L(dat);
	}
}

static void writedword(u32 dat){
	if((writelen-3)>writepos){
		writebuff[writepos++]=_32T8HH(dat);
		writebuff[writepos++]=_32T8H(dat);
		writebuff[writepos++]=_32T8L(dat);
		writebuff[writepos++]=_32T8LL(dat);
	}
}

static u16 writebuffdata(u8* buff,u16 len)
{
	u16 v=len;
	while(len--)
	{
		writebuff[writepos++]=*buff++;
		if(writelen==writepos)
			break;
	}
	return v-len;
}

static u16 writestring(u8* str){
	u16 v=writepos;
	u8 dat;
	while(writepos!=writelen)
	{
		dat=*str;
		if(dat)
			writebuff[writepos++]=dat;
		else
			break;
	}
	return writepos-v;
}

const ByteStreamBase ByteStream={
	readempty,
	readbyte,
	readword,
	readdword,
	readbuffdata,
	readstring,
	readseek,
	setreadbuff,
	getreadbuff,
	writebyte,
	writeword,
	writedword,
	writebuffdata,
	writestring,
	setwritebuff,
	getwritebuff,
};

	
