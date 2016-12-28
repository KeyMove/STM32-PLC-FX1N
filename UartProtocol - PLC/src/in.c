#include "in.h"
//#include "sys.h"
//#include "delay.h"





//PA5/4/3/2/1/0
//PB12/11/10/1/0
//PC5/4/3/2/1/0/15/14/13
void In_Init(void)
{

	RCC->APB2ENR |= RCC_APB2ENR_IOPAEN | RCC_APB2ENR_IOPBEN | RCC_APB2ENR_IOPCEN;

	GPIOA->ODR |= 0x001f;
	GPIOA->CRL &= 0xfff00000;
	GPIOA->CRL |= 0x00088888;

	GPIOB->ODR |= GPIO_ODR_ODR0 | GPIO_ODR_ODR1 | GPIO_ODR_ODR10 | GPIO_ODR_ODR11 | GPIO_ODR_ODR12;
	GPIOB->CRH &= 0xfff000ff;
	GPIOB->CRH |= 0x00088800;
	GPIOB->CRL &= 0xffffff00;
	GPIOB->CRL |= 0x00000088;

	GPIOC->ODR |= GPIO_ODR_ODR0 | GPIO_ODR_ODR1 | GPIO_ODR_ODR2 | GPIO_ODR_ODR3 | GPIO_ODR_ODR4 | GPIO_ODR_ODR5 | GPIO_ODR_ODR13 | GPIO_ODR_ODR14 | GPIO_ODR_ODR15;
	GPIOC->CRH &= 0x000fffff;
	GPIOC->CRH |= 0x88800000;
	GPIOC->CRL &= 0xff000000;
	GPIOC->CRL |= 0x00888888;

	///*GPIO_InitStructure.GPIO_Pin  = GPIO_Pin_0|GPIO_Pin_1|GPIO_Pin_2|GPIO_Pin_3|GPIO_Pin_4|GPIO_Pin_5;
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU; 
	//GPIO_Init(GPIOA, &GPIO_InitStructure);*/
	//
	//GPIO_InitStructure.GPIO_Pin  = GPIO_Pin_0|GPIO_Pin_1|GPIO_Pin_10|GPIO_Pin_11|GPIO_Pin_12;
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU; 
	//GPIO_Init(GPIOB, &GPIO_InitStructure);
	//
	//GPIO_InitStructure.GPIO_Pin  = GPIO_Pin_5|GPIO_Pin_4|GPIO_Pin_3|GPIO_Pin_2|GPIO_Pin_1|GPIO_Pin_0|GPIO_Pin_15|GPIO_Pin_14|GPIO_Pin_13;
	//GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU; 
	//GPIO_Init(GPIOC, &GPIO_InitStructure);
}


u8 In_State(u8 Number)
{
	switch (Number)
	{
	case 1:  if (!IN1) return 0; else return 1;
	case 2:  if (!IN2) return 0; else return 1;
	case 3:  if (!IN3) return 0; else return 1;
	case 4:  if (!IN4) return 0; else return 1;
	case 5:  if (!IN5) return 0; else return 1;
	case 6:  if (!IN6) return 0; else return 1;
	case 7:  if (!IN7) return 0; else return 1;
	case 8:  if (!IN8) return 0; else return 1;
	case 9:  if (!IN9) return 0; else return 1;
	case 10: if (!IN10) return 0; else return 1;
	case 11: if (!IN11) return 0; else return 1;
	case 12: if (!IN12) return 0; else return 1;
	case 13: if (!IN13) return 0; else return 1;
	case 14: if (!IN14) return 0; else return 1;
	case 15: if (!IN15) return 0; else return 1;
	case 16: if (!IN16) return 0; else return 1;
	case 17: if (!IN17) return 0; else return 1;
	case 18: if (!IN18) return 0; else return 1;
	case 19: if (!IN19) return 0; else return 1;
	case 20: if (!IN20) return 0; else return 1;
	default: return 1;
	}
}

u8 In_State_Filter(u8 Number)
{
	u8 s = 0;
	Number += 1;//数据从1开始变成从0开始
	switch (Number)
	{
	case 1:  if (!IN1) s = 1; else return 1; break;
	case 2:  if (!IN2) s = 2; else return 1; break;
	case 3:  if (!IN3) s = 3; else return 1; break;
	case 4:  if (!IN4) s = 4; else return 1; break;
	case 5:  if (!IN5) s = 5; else return 1; break;
	case 6:  if (!IN6) s = 6; else return 1; break;
	case 7:  if (!IN7) s = 7; else return 1; break;
	case 8:  if (!IN8) s = 8; else return 1; break;
	case 9:  if (!IN9) s = 9; else return 1; break;
	case 10: if (!IN10) s = 10; else return 1; break;
	case 11: if (!IN11) s = 11; else return 1; break;
	case 12: if (!IN12) s = 12; else return 1; break;
	case 13: if (!IN13) s = 13; else return 1; break;
	case 14: if (!IN14) s = 14; else return 1; break;
	case 15: if (!IN15) s = 15; else return 1; break;
	case 16: if (!IN16) s = 16; else return 1; break;
	case 17: if (!IN17) s = 17; else return 1; break;
	case 18: if (!IN18) s = 18; else return 1; break;
	case 19: if (!IN19) s = 19; else return 1; break;
	case 20: if (!IN20) s = 20; else return 1; break;
	default: return 1;
	}
	//	delay_ms(10);
	switch (s)
	{
	case 1:  if (!IN1) return 0; else return 1;
	case 2:  if (!IN2) return 0; else return 1;
	case 3:  if (!IN3) return 0; else return 1;
	case 4:  if (!IN4) return 0; else return 1;
	case 5:  if (!IN5) return 0; else return 1;
	case 6:  if (!IN6) return 0; else return 1;
	case 7:  if (!IN7) return 0; else return 1;
	case 8:  if (!IN8) return 0; else return 1;
	case 9:  if (!IN9) return 0; else return 1;
	case 10: if (!IN10) return 0; else return 1;
	case 11: if (!IN11) return 0; else return 1;
	case 12: if (!IN12) return 0; else return 1;
	case 13: if (!IN13) return 0; else return 1;
	case 14: if (!IN14) return 0; else return 1;
	case 15: if (!IN15) return 0; else return 1;
	case 16: if (!IN16) return 0; else return 1;
	case 17: if (!IN17) return 0; else return 1;
	case 18: if (!IN18) return 0; else return 1;
	case 19: if (!IN19) return 0; else return 1;
	case 20: if (!IN20) return 0; else return 1;
	}
	return 1;
}

u32 AllIn_State(void)
{
	u32 data = 0;

	if (!In_State(1)) data |= BIT(1); else data &= ~BIT(1);
	if (!In_State(2)) data |= BIT(2); else data &= ~BIT(2);
	if (!In_State(3)) data |= BIT(3); else data &= ~BIT(3);
	if (!In_State(4)) data |= BIT(4); else data &= ~BIT(4);
	if (!In_State(5)) data |= BIT(5); else data &= ~BIT(5);
	if (!In_State(6)) data |= BIT(6); else data &= ~BIT(6);
	if (!In_State(7)) data |= BIT(7); else data &= ~BIT(7);
	if (!In_State(8)) data |= BIT(8); else data &= ~BIT(8);
	if (!In_State(9)) data |= BIT(9); else data &= ~BIT(9);
	if (!In_State(10)) data |= BIT(10); else data &= ~BIT(10);
	if (!In_State(11)) data |= BIT(11); else data &= ~BIT(11);
	if (!In_State(12)) data |= BIT(12); else data &= ~BIT(12);
	if (!In_State(13)) data |= BIT(13); else data &= ~BIT(13);
	if (!In_State(14)) data |= BIT(14); else data &= ~BIT(14);
	if (!In_State(15)) data |= BIT(15); else data &= ~BIT(15);
	if (!In_State(16)) data |= BIT(16); else data &= ~BIT(16);
	if (!In_State(17)) data |= BIT(17); else data &= ~BIT(17);
	if (!In_State(18)) data |= BIT(18); else data &= ~BIT(18);
	if (!In_State(19)) data |= BIT(19); else data &= ~BIT(19);
	if (!In_State(20)) data |= BIT(20); else data &= ~BIT(20);
	//	if(!In_State(21)) data |= BIT(21);else data &= ~BIT(21);
	//	if(!In_State(22)) data |= BIT(22);else data &= ~BIT(22);
	//	if(!In_State(23)) data |= BIT(23);else data &= ~BIT(23);
	//	if(!In_State(24)) data |= BIT(24);else data &= ~BIT(24);
	//	if(!In_State_Filter(25)) data |= BIT(25);else data &= ~BIT(25);
	//	if(!In_State_Filter(26)) data |= BIT(26);else data &= ~BIT(26);
	//	if(!In_State_Filter(27)) data |= BIT(27);else data &= ~BIT(27);
	//	if(!In_State_Filter(28)) data |= BIT(28);else data &= ~BIT(28);
	//	if(!In_State_Filter(29)) data |= BIT(29);else data &= ~BIT(29);
	//	if(!In_State_Filter(30)) data |= BIT(30);else data &= ~BIT(30);
	return (data >> 1);
}


u8 Read_Bit(u16 address)
{
	u8 res;
	u16 buf_number = 0;
	buf_number = address & 0x000f;
	buf_number |= (address & 0xff00) >> 4;
	res = (!In_State_Filter(buf_number));
	return res;
}



u16 Read_HalfWord(u8 address)
{
	s8 i = 0;
	u16 res = 0, resbuf = 0;
	address <<= 4;
	for (i = 15; i >= 0; i--)
	{
		res <<= 1;
		res |= (!In_State(address + i));
	}
	//	delay_ms(50);
	for (i = 15; i >= 0; i--)
	{
		resbuf <<= 1;
		resbuf |= (!In_State(address + i));
	}
	return (resbuf&res);
}







