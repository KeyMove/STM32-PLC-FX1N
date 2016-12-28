#ifndef __OUT_H
#define __OUT_H
//#include "sys.h"
#include"mcuhead.h"

#define OUT1_SET() SETBIT(GPIOC->ODR,BIT11)
#define OUT1_CLR() CLRBIT(GPIOC->ODR,BIT11)

#define OUT2_SET() SETBIT(GPIOC->ODR,BIT10)
#define OUT2_CLR() CLRBIT(GPIOC->ODR,BIT10)

#define OUT3_SET() SETBIT(GPIOA->ODR,BIT12)
#define OUT3_CLR() CLRBIT(GPIOA->ODR,BIT12)

#define OUT4_SET() SETBIT(GPIOA->ODR,BIT11)
#define OUT4_CLR() CLRBIT(GPIOA->ODR,BIT11)

#define OUT5_SET() SETBIT(GPIOA->ODR,BIT8)
#define OUT5_CLR() CLRBIT(GPIOA->ODR,BIT8)

#define OUT6_SET() SETBIT(GPIOC->ODR,BIT9)
#define OUT6_CLR() CLRBIT(GPIOC->ODR,BIT9)

#define OUT7_SET() SETBIT(GPIOC->ODR,BIT8)
#define OUT7_CLR() CLRBIT(GPIOC->ODR,BIT8)

#define OUT8_SET() SETBIT(GPIOC->ODR,BIT7)
#define OUT8_CLR() CLRBIT(GPIOC->ODR,BIT7)

#define OUT9_SET() SETBIT(GPIOC->ODR,BIT6)
#define OUT9_CLR() CLRBIT(GPIOC->ODR,BIT6)

#define OUT10_SET() SETBIT(GPIOB->ODR,BIT15)
#define OUT10_CLR() CLRBIT(GPIOB->ODR,BIT15)

#define OUT11_SET() SETBIT(GPIOB->ODR,BIT14)
#define OUT11_CLR() CLRBIT(GPIOB->ODR,BIT14)

#define OUT12_SET() SETBIT(GPIOB->ODR,BIT13)
#define OUT12_CLR() CLRBIT(GPIOB->ODR,BIT13)





void Out_Init(void);
void Out_State(u8 Number,u8 state);
void Write_Bit(u16 address,u8 state);
void Write_HalfWord(u8 address,u16 state);

#endif
