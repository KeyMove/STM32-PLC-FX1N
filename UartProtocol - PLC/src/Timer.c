#include"Timer.H"

const TimerBase Timer={
	InitTimer,
	TimerRun,
	KillTimer,
	KillThisTimer,
	isUse,
	SetTimer,
	Delay,
	{
		AddDelay,
		SetDelay,
		ZeroDelay
	},
	{
		AddTargetDelay,
		SetTargetDelay,
		ZeroTargetDelay,
	}
};

_Timer TimerList[MAXTIMER];	//定时器列表
u8 this;										//当前执行
TimerBit TimerUse;					//使用槽
u32 InputSP;

//初始化定时器
void InitTimer(u8 mhz)			
{
	mhz/=8;
	SysTick->CTRL	&=	0xfffffffb;	//sysclock/8
	SysTick->LOAD	=		(int)mhz*1000;				//1000us timer
	SysTick->VAL	=		0x00;
	SysTick->CTRL	=		3;					//start timer
}
//定时器中断
void SysTick_Handler(void)
{
	u8 i;
	for(i=0;i<MAXTIMER;i++)
		if(TimerList[i].nTime)
			TimerList[i].nTime--;
}
//初始化创建定时器
u8 SetTimer(u16 time,TimerCallBack fun)
{
	TimerBit BIT=1;
	u8 i;
	for(i=0;i<MAXTIMER;i++)
	{
		if(!(BIT&TimerUse))
		{
			TimerUse|=BIT;
			TimerList[i].fun=fun;
			TimerList[i].nTime=0;
			TimerList[i].time=time;
			return i;
		}
		BIT<<=1;
	}
	return 255;
}
//接结束当前定时器
void KillThisTimer(void)
{
	TimerBit BIT=1;
	u8 i;
	for(i=0;i<this;i++)
		BIT<<=1;
	TimerUse&=~BIT;
}
//接结束指定定时器
void KillTimer(u8 id)
{
	TimerBit BIT=1;
	u8 i;
	for(i=0;i<id;i++)
		BIT<<=1;
	TimerUse&=~BIT;
}
//改变定时器时间
void SetDelay(u16 time)
{
	TimerList[this].time = time;
}
//改变定时器时间
void SetTargetDelay(u8 id,u16 time)
{
	TimerList[id].time = time;
}
//修改单次定时器时间
void AddDelay(u16 time)
{
	TimerList[this].nTime+=time;
}

void AddTargetDelay(u8 id,u16 time)
{
	TimerList[id].nTime += time;
}

void ZeroTargetDelay(u8 id)
{
	TimerList[id].nTime = 0;
}

void ZeroDelay(void)
{
	TimerList[this].nTime=0;
}

u8 isUse(u8 id)
{
	if(TimerUse&(1<<id))
		return 1;
	return 0;
}

__ASM u32 GetSP(void)
{
	mov r0,sp
	bx lr
}

__ASM u32 GetBreakAddr(void)
{ 
	nop
	mov r0,sp								;取SP指针地址
	ldr r1,=__cpp(&InputSP) ;获取全局变量InputSP地址
	ldr r1,[r1,#0]					;获取地址内数据
	subs r0,r1,r0           ;当前SP地址减去起始SP地址
	cmp r0,#8								;检测是否中途恢复
	beq n                   ;如果为中途恢复取消R0-8
	subs r0,#8							;R0-8修正返回地址
n	
	mov r1,sp								;重新获取SP指针地址
	adds r1,r0,r1						;加上偏移量
	pop {r0}								;R4出栈
	pop {r0}								;函数返回地址出栈
	mov sp,r1								;设置SP地址
	bx lr										;函数返回
}

void CallFun(u32 addr)
{
	(*(void(*)())addr)();
}

void Delay(u16 time)
{
	TimerList[this].breakAddr.bAddr=GetBreakAddr();
	TimerList[this].nTime+=time;
}

//运行已经回调函数
void TimerRun(void)
{
	static TimerBit BIT;
	u32 fun;
	BIT=1;
	for(this=0;this<MAXTIMER;this++)
	{
		if(BIT&TimerUse)
		{
			if(!TimerList[this].nTime)
			{
				TimerList[this].nTime=TimerList[this].time;
				InputSP=GetSP();
				if(TimerList[this].breakAddr.bAddr)
				{
					fun=TimerList[this].breakAddr.bAddr;
					TimerList[this].breakAddr.bAddr=0;
					CallFun(fun);
				}
				else
				{
					TimerList[this].fun();
				}
			}
		}
		BIT<<=1;
	}
}

