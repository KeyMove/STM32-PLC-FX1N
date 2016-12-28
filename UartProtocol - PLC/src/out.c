#include "out.h"
//#include "sys.h"


void Out_Init(void)
{

	RCC->APB2ENR |= RCC_APB2ENR_IOPAEN | RCC_APB2ENR_IOPBEN | RCC_APB2ENR_IOPCEN;

	GPIOA->ODR |= GPIO_ODR_ODR8 | GPIO_ODR_ODR11 | GPIO_ODR_ODR12;
	GPIOA->CRH &= 0xfff00ff0;
	GPIOA->CRH |= 0x00033003; 

	GPIOB->ODR |= GPIO_ODR_ODR13 | GPIO_ODR_ODR14 | GPIO_ODR_ODR15;
	GPIOB->CRH &= 0x000fffff;
	GPIOB->CRH |= 0x33300000;

	GPIOC->ODR |= GPIO_ODR_ODR6 | GPIO_ODR_ODR7 | GPIO_ODR_ODR8 | GPIO_ODR_ODR9 | GPIO_ODR_ODR10 | GPIO_ODR_ODR11;
	GPIOC->CRH &= 0xffff0000;
	GPIOC->CRH |= 0x00003333;
	GPIOC->CRL &= 0x00ffffff;
	GPIOC->CRL |= 0x33000000;
	
	//GPIO_InitStructure.GPIO_Pin = GPIO_Pin_8|GPIO_Pin_11|GPIO_Pin_12;	//pa8/11/12
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
	//GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
	//GPIO_Init(GPIOA,&GPIO_InitStructure);
	//GPIO_SetBits(GPIOA,GPIO_Pin_8|GPIO_Pin_11|GPIO_Pin_12);	


	//GPIO_InitStructure.GPIO_Pin = GPIO_Pin_13|GPIO_Pin_14|GPIO_Pin_15;		//pb13/14/15	
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP; 		
	//GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;	
	//GPIO_Init(GPIOB, &GPIO_InitStructure);					
	//GPIO_SetBits(GPIOB,GPIO_Pin_13|GPIO_Pin_14|GPIO_Pin_15);

	//GPIO_InitStructure.GPIO_Pin = GPIO_Pin_6|GPIO_Pin_7|GPIO_Pin_8|GPIO_Pin_9|GPIO_Pin_10|GPIO_Pin_11;		//pc6/7/8/9/10/11
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
	//GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
	//GPIO_Init(GPIOC,&GPIO_InitStructure);
	//GPIO_SetBits(GPIOC,GPIO_Pin_6|GPIO_Pin_7|GPIO_Pin_8|GPIO_Pin_9|GPIO_Pin_10|GPIO_Pin_11);

	
}
//state:0动作，1复位
void Out_State(u8 Number,u8 state)
{
	//if(state) state=1;
	Number += 1;//数据从1开始变成从0开始
		switch(Number)
		{
			case 1: if(state)OUT1_SET(); else OUT1_CLR();break;
			case 2: if(state)OUT2_SET(); else OUT2_CLR();break; 
			case 3: if(state)OUT3_SET(); else OUT3_CLR();break;
			case 4: if(state)OUT4_SET(); else OUT4_CLR();break;
			case 5: if(state)OUT5_SET(); else OUT5_CLR();break;
			case 6: if(state)OUT6_SET(); else OUT6_CLR();break;
			case 7: if(state)OUT7_SET(); else OUT7_CLR();break;
			case 8: if(state)OUT8_SET(); else OUT8_CLR();break;
			case 9: if(state)OUT9_SET(); else OUT9_CLR();break;
			case 10: if(state)OUT10_SET(); else OUT10_CLR();break;
			case 11: if(state)OUT11_SET(); else OUT11_CLR();break;
			case 12: if(state)OUT12_SET(); else OUT12_CLR();break;
		}
}

void Write_Bit(u16 address,u8 state)
{
	u16 buf_number=0;
	buf_number = address&0x000f;
	buf_number|=(address&0xff00)>>4;
	Out_State(buf_number,!state);
}
void Write_HalfWord(u8 address,u16 state)
{
	s8 i=0;
//	u16 res=0;
	address <<= 4;
	for(i=0;i<16;i++)
	{
		Out_State((address+i),!(state&0x0001));
		state >>= 1;
	}	
}





