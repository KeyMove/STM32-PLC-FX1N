#include"sled.h"

u8 LED_BUFF[2];
u8 LED_Value;
u8 BlinkBit;
u8 PointBit;
void (*FunctionCallBack)(void);
static const u8 seg_table[]={
								0xC0,  //"0"
                0xF9,  //"1"
                0xA4,  //"2"
                0xB0,  //"3"
                0x99,  //"4"
                0x92,  //"5"
                0x82,  //"6"
                0xF8,  //"7"
                0x80,  //"8"
                0x90,  //"9"
								0x88,  //"A"
								0x83,  //"B"
                0xC6,  //"C"
                0xA1,  //"D"
                0x86,  //"E"
                0x8E,  //"F"
};

static void senddata(u8 dat)
{
	u8 i;
	for(i=0;i<8;i++)
	{
		if(dat&0x80)
			SLED_SDA_SET();
		else
			SLED_SDA_CLR();
		SLED_CLK_SET();
		dat<<=1;
		SLED_CLK_CLR();
	}
	SLED_OE_CLR();
	SLED_OE_SET();
	SLED_OE_CLR();
}

static void InitGPIO(void)
{
	//SD PB3
	//OE PB4
	//CK PB5

	//B1 PD2
	//B2 PC12
	
	RCC->APB2ENR|=RCC_APB2ENR_IOPBEN|RCC_APB2ENR_IOPCEN|RCC_APB2ENR_IOPDEN;
	GPIOB->CRL&=0xff000fff;
	GPIOB->CRL|=0x00333000;
	
	GPIOD->CRL&=0xfffff0ff;
	GPIOD->CRL|=0x00000300;
	
	GPIOC->CRH&=0xfff0ffff;
	GPIOC->CRH|=0x00030000;
	
	RCC->APB1ENR|=RCC_APB1ENR_TIM2EN;
	
	TIM2->ARR=1000-1;
	TIM2->PSC=72-1;
	
	TIM2->CR1|=TIM_CR1_CEN;
	TIM2->DIER|=TIM_DIER_UIE;
	
	NVIC_SetPriority(TIM2_IRQn,2);
	NVIC_EnableIRQ(TIM2_IRQn);
	
	BlinkBit=LED_Value=0;
	FunctionCallBack=0;
}


static u16 blinkcount;
#define blinktime 800
void TIM2_IRQHandler(void)
{
	static u8 sw;
	
	CLRBIT(TIM2->SR,TIM_SR_UIF);
	do{
	if(sw){
		if((BlinkBit&B1)&&(blinkcount>blinktime/2))
		{
			SLED_B1_CLR();
			break;
		}
		SLED_B2_CLR();
		if(PointBit&B1)
			senddata(seg_table[LED_BUFF[0]]|0x80);
		else
			senddata(seg_table[LED_BUFF[0]]);
		SLED_B1_SET();
	}
	else{
		if((BlinkBit&B2)&&(blinkcount>blinktime/2))
		{
			SLED_B2_CLR();
			break;
		}
		SLED_B1_CLR();
		if(PointBit&B2)
			senddata(seg_table[LED_BUFF[1]]|0x80);
		else
			senddata(seg_table[LED_BUFF[1]]);
		SLED_B2_SET();
	}
	}while(0);
	
	if(++blinkcount==blinktime)
	{
		blinkcount=0;
	}
	
	sw=!sw;
	if(FunctionCallBack!=0)
		FunctionCallBack();
}

static u8 get()
{
	return LED_Value;
}

static void set(u8 dat){
	blinkcount=0;
	LED_BUFF[0]=dat%100/10;
	LED_BUFF[1]=dat%10;
}

static void sethex(u8 dat)
{
	blinkcount=0;
	LED_BUFF[0]=(dat>>4)&0x0f;
	LED_BUFF[1]=dat&0x0f;
}

static void blink(u8 blink)
{
	BlinkBit=blink;
}

static void point(u8 p)
{
	PointBit=p;
}

static void Enable(u8 e)
{
	if(e)
	{
		TIM2->CR1|=TIM_CR1_CEN;
	}
	else
	{
		CLRBIT(TIM2->CR1,TIM_CR1_CEN);
		SLED_B1_CLR();
		SLED_B2_CLR();
	}
}

static void timecall(void(*function)(void))
{
	FunctionCallBack=function;
}
	

const SLEDBase SLED={
	InitGPIO,
	set,
	sethex,
	get,
	blink,
	point,
	Enable,
	timecall,
};
