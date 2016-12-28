#ifndef __IN_H
#define __IN_H
//#include "sys.h"
#include "mcuhead.h"

#define BIT(n) (1<<(n))



//PA5/4/3/2/1/0
//PB12/11/10/1/0
//PC5/4/3/2/1/0/15/14/13
#define IN1		(GPIOB->IDR&BIT12)    
#define IN2		(GPIOB->IDR&BIT11)	
#define IN3		(GPIOB->IDR&BIT10)	
#define IN4		(GPIOB->IDR&BIT1)
#define IN5		(GPIOB->IDR&BIT0)
#define IN6		(GPIOC->IDR&BIT5)
#define IN7		(GPIOC->IDR&BIT4)
#define IN8		(GPIOA->IDR&BIT5)
#define IN9		(GPIOA->IDR&BIT4)
#define IN10	(GPIOA->IDR&BIT3)
#define IN11	(GPIOA->IDR&BIT2)
#define IN12	(GPIOA->IDR&BIT1)
#define IN13	(GPIOA->IDR&BIT0)
#define IN14	(GPIOC->IDR&BIT3)
#define IN15	(GPIOC->IDR&BIT2)
#define IN16	(GPIOC->IDR&BIT1)
#define IN17	(GPIOC->IDR&BIT0)
#define IN18	(GPIOC->IDR&BIT15)
#define IN19	(GPIOC->IDR&BIT14)
#define IN20	(GPIOC->IDR&BIT13)


void In_Init(void);
u8 In_State_Filter(u8 Number);
u32 AllIn_State(void);
u8 Read_Bit(u16 address);
u16 Read_HalfWord(u8 address);
u8 In_State(u8 Number);



#endif
