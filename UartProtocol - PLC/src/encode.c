#include"encode.h"

EncodeCallBack ENCALL;

void Ex_NVIC_Config(u8 GPIOx,u8 BITx,u8 TRIM) 
{
	u8 EXTADDR;
	u8 EXTOFFSET;
	EXTADDR=BITx/4;//得到中断寄存器组的编号
	EXTOFFSET=(BITx%4)*4; 
	RCC->APB2ENR|=0x01;//使能io复用时钟			 
	AFIO->EXTICR[EXTADDR]&=~(0x000F<<EXTOFFSET);//清除原来设置！！！
	AFIO->EXTICR[EXTADDR]|=GPIOx<<EXTOFFSET;//EXTI.BITx映射到GPIOx.BITx 
	//自动设置
	EXTI->IMR|=1<<BITx;//  开启line BITx上的中断
	//EXTI->EMR|=1<<BITx;//不屏蔽line BITx上的事件 (如果不屏蔽这句,在硬件上是可以的,但是在软件仿真的时候无法进入中断!)
 	if(TRIM&0x01)EXTI->FTSR|=1<<BITx;//line BITx上事件下降沿触发
	if(TRIM&0x02)EXTI->RTSR|=1<<BITx;//line BITx上事件上升降沿触发
} 

static void InitGPIO()
{
	RCC->APB2ENR|=RCC_APB2ENR_IOPAEN|RCC_APB2ENR_IOPBEN;
	GPIOA->ODR|=BIT7|BIT6;
	GPIOA->CRL&=0x00ffffff;
	GPIOA->CRL|=0x88000000;
	
	GPIOB->ODR|=BIT9;
	GPIOB->CRH&=0xffffff0f;
	GPIOB->CRH|=0x00000080;
	
	Ex_NVIC_Config(0,6,1);
	Ex_NVIC_Config(0,7,1);
	Ex_NVIC_Config(1,9,1);
	
	NVIC_SetPriority(EXTI9_5_IRQn,2);
	NVIC_EnableIRQ(EXTI9_5_IRQn);
}

static void Init(EncodeCallBack f)
{
	ENCALL=f;
	InitGPIO();
}

static u8 rl;
static u8 rr;
static u8 dis;
void EXTI9_5_IRQHandler(void){
	
	if (dis == 0) {
		if (EXTI->PR&RR)
		{
			EXTI->PR = RR;
			if (rl && (!(GPIOA->IDR&RL)))
			{
				dis = 5;
				rr = rl = 0;
				ENCALL(RotL);
			}
			else
			{
				rr = 1;
			}
		}

		if (EXTI->PR&RL)
		{
			EXTI->PR = RL;
			if (rr && (!(GPIOA->IDR&RR)))
			{
				dis = 5;
				rr = rl = 0;
				ENCALL(RotR);
			}
			else
			{
				rl = 1;
			}
		}
	}
	if(EXTI->PR&DD)
	{
		rr=rl=0;
		EXTI->PR=DD;
		ENCALL(RDown);
	}
}

static void TimeCheck(void){
	if((GPIOA->IDR&RR)&&(GPIOA->IDR&RL))
	{
		rr=rl=0;
	}
	if(dis)
	dis--;
}

const EnCodeBase encode={
	Init,
	TimeCheck,
};
