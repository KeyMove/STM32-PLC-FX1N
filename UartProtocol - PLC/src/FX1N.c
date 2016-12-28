#include "FX1N.h"

//typedef struct{
//	u8 S;
//	u8 X;
//	u8 Y;
//	u8 T;
//	u8 M;
//	u8 C;
//}TargetType;


//typedef struct{
//	u8 Other;
//	u8 LD;
//	u8 LDI;
//	u8 AND;
//	u8 ANDI;
//	u8 OR;
//	u8 ORI;
//	u8 OUT;
//	u8 SET;
//	u8 Adv;
//}CmdType1;

//static enum{
//	Other_=0,
//	LD_=2,
//	LDI_=3,
//	AND_=4,
//	ANI_=5,
//	OR_=6,
//	ORI_=7,
//	OUT_=0xc,
//	SET_=0xd,
//	adv_=0xf,
//	
//}CmdType;



typedef struct{
u8 *S;
u8 *X;
u8 *Y;
u8 *T;
u8 *M;
u8 *C;
u8 *MX;
u16* D;
}Reg;

static Reg Register;//主寄存器
static Reg BackupRegister;//备份寄存器  用于判断上升沿

static u16 *TimerValue;//定时器值 BIT15为使用标志位
static u16 *CountValue;//计数器值

static u8* databuff;//变量缓存地址
static u16 p;//点数据
static u16 PC;//程序位置指针
static u8 *code;//程序数据
static u8 Stack;//栈
static u8 BackupStack;//备份栈

static const u8 bits[]={1,2,4,8,0x10,0x20,0x40,0x80};

//---PLS--PLF
static u16 PlsIndex;
static u16 PlsStatus;
static u16 lastPlsStatus;

//callback
IOCallBack ReadIO=0;
IOCallBack WriteIO=0;

const u8 FNC_CMD_LENGHT[]={
1,1,0,0,0,0,0,0,1,0,
          3,4,2,5,2,3,3,2,2,2,
          3,3,3,3,1,1,3,3,3,1,
          2,2,2,2,4,4,4,4,3,3,
          2,3,3,2,3,3,3,0,2,2,
          2,1,4,3,3,4,3,3,3,4,
          3,4,4,4,2,3,1,4,4,5,
          3,4,4,2,3,4,2,2,4,4,
          4,2,3,3,3,2,2,0,4,0,
          0,0,0,0,0,0,0,0,0,0,
          0,0,0,0,0,0,0,0,0,0,
          3,4,0,0,0,0,0,0,2,2,
          3,3,3,3,0,0,0,2,0,2,
          2,2,2,0,0,0,0,0,0,0,
          0,0,0,0,0,0,0,1,0,0,
          0,0,0,0,0,3,4,3,4,4,
          5,4,3,3,0,0,1,1,0,3,
          2,2,0,0,0,0,3,3,0,0,
          0,0,0,0,0,0,0,0,0,0,
          0,0,0,0,0,0,0,0,0,0,
          0,0,0,0,0,0,0,0,0,0,
          0,0,0,0,0,0,0,0,0,0,
          0,0,0,0,2,2,2,0,2,2,
          2,0,2,2,2,0,2,2,2,0,
          2,2,2,0,2,2,2,0,0,0,
          0,0,0,0,0,0       
};


static void SetBuffBit(u8* buff,u16 point,u8 value)
{
	if(value)
		buff[point>>3]|=bits[point&7];
	else
		buff[point>>3]&=~bits[point&7];
}

static u8 GetBuffBit(u8* buff,u16 point){
	return (buff[point>>3]&bits[point&7])!=0;
}

typedef void(*Action)();

static u8 ReadPoint(Reg* Register,u16 point)
{
	
	switch(_16T8H(point)&0x0f)
	{
		case 0:
		case 1:
		case 2:
		case 3:
			if(point<S_Point)
				return GetBuffBit(Register->S,point);
			break;
		case 4:
			if(_16T8L(point)<X_Point)
				return GetBuffBit(Register->X,_16T8L(point));
			break;
		case 5:
			if(_16T8L(point)<Y_Point)
				return GetBuffBit(Register->Y,_16T8L(point));
			break;
		case 6:
			if(_16T8L(point)<T_Point)
				return GetBuffBit(Register->T,_16T8L(point));
			break;
		case 7:			
			break;
		case 8:
		case 9:
		case 0xA:
		case 0xB:
		case 0xC:
		case 0xD:
			_16T8H(point)-=0x8;
			if(point<M_Point)
				return GetBuffBit(Register->M,point);
			break;
		case 0xE:
			_16T8H(point)-=0xe;
			if(point<C_Point)
				return GetBuffBit(Register->C,_16T8L(point));
			break;
		case 0xF:
			_16T8H(point)-=0xf;
			if(point<MX_Point)
				return GetBuffBit(Register->MX,point);
			break;
	}
	return 0;
}


void SetYM(u16 point,u8 value)
{
	value&=BIT0;
	if(_16T8H(point)==0x05){
		if(_16T8L(point)<Y_Point)		
			SetBuffBit(Register.Y,_16T8L(point),value);
	}
	else if(_16T8H(point)>=8&&_16T8H(point)<=0x0D){
			_16T8H(point)-=0x8;
			if(point<M_Point)
				 SetBuffBit(Register.M,point,value);
	}
}

void SetMX(u16 point,u8 value)
{
	value&=BIT0;
	if(point<MX_Point)
		SetBuffBit(Register.MX,point,value);
}
void SetS(u16 point,u8 value)
{
	value&=BIT0;
	if(point<S_Point)
		SetBuffBit(Register.S,point,value);
}

void SetD(u16 point,u8 value)
{
	value&=BIT0;
	if(point<S_Point)
		SetBuffBit(Register.S,point,value);
}

void SetC(u16 point,u8 value)
{
	value&=BIT0;
	if(point<C_Point)
	{
		SetBuffBit(Register.C,point,value);
		if(!value)CountValue[point]=0;
	}
}

void SetT(u16 point,u8 value)
{
	value&=BIT0;
	if(point<T_Point)
	{
		SetBuffBit(Register.T,point,value);
		if(!value)TimerValue[point]=0;
	}
}

void NoUSE(){}

//--------------------------------------------------------------------------------------------//
void LDP(){
	Stack<<=1;
	Stack|=((ReadPoint(&Register, p)&~ReadPoint(&BackupRegister, p))&BIT0);
}
void LDF(){
	Stack<<=1;
	Stack|=((~ReadPoint(&Register, p)&ReadPoint(&BackupRegister, p))&BIT0);
}
void ANDP(){
	Stack&=((ReadPoint(&Register, p)&~ReadPoint(&BackupRegister, p))&0xfe);
}
void ANDF(){
	Stack&=((~ReadPoint(&Register, p)&ReadPoint(&BackupRegister, p))&0xfe);
}
void ORP(){
	Stack|=((ReadPoint(&Register, p)&~ReadPoint(&BackupRegister, p))&BIT0);
}
void ORF(){
	Stack|=((~ReadPoint(&Register, p)&~ReadPoint(&BackupRegister, p))&BIT0);
}
const Action FPCMD[]={
	LDP,
	LDF,
	ANDP,
	ANDF,
	ORP,
	ORF,
};
//--------------------------------------------------------------------------------------------------//


//--------------------------------------------------------------------------------------------------//
void ANB(){
	Stack=(Stack>>1)&((Stack&BIT0)|0xfe);
}
void ORB(){
	Stack=(Stack>>1)|(Stack&BIT0);
}
void MPS(){
	BackupStack=(BackupStack<<1)|(Stack&BIT0);
}
void MDR(){
	Stack=(Stack&0xfe)|(BackupStack&BIT0);
}
void MPP(){
	MDR();
	BackupStack>>=1;
}
void INV(){
	CPLBIT(Stack,BIT0);
}
void NOP(){}

const Action ByteCMD[]={
	ANB,
	ORB,
	MPS,
	MDR,
	MPP,
	INV,
	NOP,
};
//--------------------------------------------------------------------------------------------------//


void OtherAction()
{
	u16 point;
	if(p==0x0f){
		PC=0;
	}
	else if(p<0x0e)
	{
		point=p;
		if(!(p<=1||p==0xa||p==0xd||p==0xe))
		{
			_16T8L(p)=code[PC++];
			_16T8H(p)=code[PC++];
			if(!(p&0x8000))
				return;
			p&=0xfff;
		}
		switch(point)
		{
			case 0:break;
			case 1:break;
			case 2://OUT M8xxx
				SetMX(p,Stack);
				break;
			case 3://SET M8xxx
				if(Stack&BIT0)
					SetMX(p,1);	
				break;
			case 4://RST M8xxx
				if(Stack&BIT0)
					SetMX(p,0);
				break;
			case 5://OUT Sx
				SetS(p,Stack);
				break;
			case 6://SET Sx
				if(Stack&BIT0)
					SetS(p,1);
				break;
			case 7://RST Sx
				if(Stack&BIT0)
					SetS(p,0);
				break;
			case 8://PLS
				if(Stack&BIT0){
					PlsStatus|=PlsIndex;
					if(PlsStatus&lastPlsStatus)
						SetYM(p,0);
					else 
						SetYM(p,1);
				}
				else{
					PlsStatus&=~PlsIndex;
					SetYM(p,0);
				}
				PlsIndex<<=1;
				break;
			case 9://PLF
				if(Stack&BIT0){
					PlsStatus|=PlsIndex;
					SetYM(p,0);
				}					
				else{
					PlsStatus&=~PlsIndex;
					if(PlsStatus&lastPlsStatus)
						SetYM(p,0);
					else
						SetYM(p,1);
				}
				break;
			case 0xa://MC
				break;
			case 0xb://MCR
				break;
			case 0xc://RSTTC
				if(_16T8H(p)==0x6){
					if(Stack&BIT0)
						SetT(_16T8L(p),0);
				}
				else if(_16T8H(p)==0xe){
					if(Stack&BIT0)
						SetC(_16T8L(p),0);
				}
				break;
			case 0xd://RSTD
				_16T8L(p)=code[PC++];PC++;
				_16T8H(p)=code[PC++];
				if(!(Stack&BIT0))break;
				switch(code[PC++]&0xf)
				{
					case 0://RST D8000+
						break;
					case 2://RST T0-T256
						SetT(p,0);
						break;
					case 4://RST C0-C234
						SetC(p,0);
						break;
					case 6://RST D0-D1998
						if(p<D_Point)
							Register.D[p]=0;
						break;
					case 8://RST D1000+
						break;
				}
				break;
			case 0xe:break;
		}
	}
	else if((p>=0x1CA)&&(p<=0x1CF)){
		point=(p&0xf)-0xa;
		_16T8L(p)=code[PC++];
		_16T8H(p)=code[PC++];
		p&=0xfff;
		FPCMD[point]();
	}
	else if((_16T8H(p)==0x06)||(_16T8H(p)==0x0e)){
		point=p;
		_16T8L(p)=code[PC++];PC++;
		_16T8H(p)=code[PC++];
		if(code[PC++]==0x86)//D
		{
			if(p>=D_Point)return;
			p=Register.D[p];
		}
		if(_16T8L(point)>=T_Point)return;
		if(_16T8H(point)==0x06)
		{
			if((Stack&BIT0))
			{
				if(TimerValue[_16T8L(point)]==0)
				{
					SetT(_16T8L(point),0);
					TimerValue[_16T8L(point)]=p|BIT15;
				}
			}
			else
			{
				SetT(_16T8L(point),0);
			}
		}
		else 
			if((Stack&BIT0))
				if(++CountValue[_16T8L(point)]==p)
					SetC(_16T8L(point),1);
	}
	else if((p&1)==0){
		if((point=((p>>1)-8))>256)return;
		PC+=FNC_CMD_LENGHT[point];
	}
}

void LD(){
	Stack<<=1;
	Stack|=ReadPoint(&Register, p);
}

void LDI(){
	Stack<<=1;
	Stack|=(~ReadPoint(&Register,p))&BIT0;
}

void AND(){
	Stack&=ReadPoint(&Register,p)|0xfe;
}
void ANI(){
	Stack&=(~ReadPoint(&Register,p))|0xfe;
}
void OR(){
	Stack|=ReadPoint(&Register,p);
}
void ORI(){
	Stack|=(~ReadPoint(&Register,p))&1;
}

void OUTYM(){
	SetYM(p,Stack);
}
void SETYM(){
	if(Stack&BIT0)SetYM(p,1);
}
void RSTYM(){
	if(Stack&BIT0)SetYM(p,0);
}

void ADV(){
	u8 index=_16T8L(p)&0xf;
	if(index<8)
		return;
	ByteCMD[index-8]();
}

const Action CMD[]={
	OtherAction,
	NoUSE,
	LD,
	LDI,
	AND,
	ANI,
	OR,
	ORI,
	NoUSE,
	NoUSE,
	NoUSE,
	NoUSE,
	OUTYM,
	SETYM,
	RSTYM,
	ADV,
};



//----------------------------------------------------------------//


static u8 LoadCode(u8* codebuff)
{
	code=codebuff;
	return 1;
}

static void Update()
{
	u16 i;
	u8* p=BackupRegister.S;
	for(i=0;i<p-databuff;i++)
	{
		p[i]=databuff[i];
	}
	if(ReadIO)
		ReadIO(X_Point,Register.X);
	if(WriteIO)
		WriteIO(Y_Point,Register.Y);
}


static void RunCode()
{
	u8 index;
	if(code==0)return;
	PlsIndex=1;
	do
	{
		_16T8L(p)=code[PC++];
		index=_16T8H(p)=code[PC++];
		p&=0xfff;
		index>>=4;
		CMD[index]();
	}while(PC);
	lastPlsStatus=PlsStatus;
	Update();
}

static void Reset(){
	u16 i;
	for(i=0;i<FX1N_BUFFSIZE;i++)
		databuff[i]=0;
	PC=0;
	lastPlsStatus=0;
}

static void UpdateTimer100ms(){
	u8 i;
	for(i=0;i<T_Point;i++)
		if(TimerValue[i]&~BIT15)
			if(--TimerValue[i]==BIT15)
				SetT(i,1);	
}

static void SetIO(IOCallBack r,IOCallBack w){
	ReadIO=r;
	WriteIO=w;
}

static void init(u8* runbuff){
	u16 i;
	u8* r1=runbuff;
	databuff=runbuff;
	
	Register.S=r1;r1+=POINTSIZE(S_Point);
	Register.X=r1;r1+=POINTSIZE(X_Point);
	Register.Y=r1;r1+=POINTSIZE(Y_Point);
	Register.T=r1;r1+=POINTSIZE(T_Point);
	Register.M=r1;r1+=POINTSIZE(M_Point);
	Register.C=r1;r1+=POINTSIZE(C_Point);
	Register.MX=r1;r1+=POINTSIZE(MX_Point);
	
	BackupRegister.S=r1;r1+=POINTSIZE(S_Point);
	BackupRegister.X=r1;r1+=POINTSIZE(X_Point);
	BackupRegister.Y=r1;r1+=POINTSIZE(Y_Point);
	BackupRegister.T=r1;r1+=POINTSIZE(T_Point);
	BackupRegister.M=r1;r1+=POINTSIZE(M_Point);
	BackupRegister.C=r1;r1+=POINTSIZE(C_Point);
	BackupRegister.MX=r1;r1+=POINTSIZE(MX_Point);
	
	Register.D=(u16*)r1;r1+=D_Point*sizeof(u16);
	TimerValue=(u16*)r1;r1+=T_Point*sizeof(u16);
	CountValue=(u16*)r1;r1+=C_Point*sizeof(u16);
	
	for(i=0;i<FX1N_BUFFSIZE;i++)
		databuff[i]=0;
}




const FX1NBase FX1N = {
	init,	
	LoadCode,
	Reset,
	SetIO,
	RunCode,
	UpdateTimer100ms,
};
