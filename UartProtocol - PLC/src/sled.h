#ifndef _sled_H_
#define _sled_H_

#include"mcuhead.h"

//PB PB3
//PB PB4
//PB PB5

//B1 PD2
//B2 PC12


//SD PB3
//OE PB4
//CK PB5

//B1 PD2
//B2 PC12

#define SLED_SDA BIT3
#define SLED_OE  BIT4
#define SLED_CLK BIT5

#define SLED_B1  BIT2
#define SLED_B2  BIT12

#define SLED_SDA_SET() SETBIT(GPIOB->ODR,SLED_SDA)
#define SLED_SDA_CLR() CLRBIT(GPIOB->ODR,SLED_SDA)

#define SLED_OE_SET() SETBIT(GPIOB->ODR,SLED_OE)
#define SLED_OE_CLR() CLRBIT(GPIOB->ODR,SLED_OE)

#define SLED_CLK_SET() SETBIT(GPIOB->ODR,SLED_CLK)
#define SLED_CLK_CLR() CLRBIT(GPIOB->ODR,SLED_CLK)

#define SLED_B1_SET() SETBIT(GPIOD->ODR,SLED_B1)
#define SLED_B1_CLR() CLRBIT(GPIOD->ODR,SLED_B1)

#define SLED_B2_SET() SETBIT(GPIOC->ODR,SLED_B2)
#define SLED_B2_CLR() CLRBIT(GPIOC->ODR,SLED_B2)

typedef enum{
	B1=1,
	B2=2,
}BlinkType;

typedef struct{
	void(*Init)(void);
	void(*SetValue)(u8);
	void(*SetHex)(u8);
	u8(*GetValue)(void);
	void(*BLink)(u8);
	void(*Point)(u8);
	void(*Enable)(u8);
	void(*Timer1MS)(void(*function)(void));
}SLEDBase;

extern const SLEDBase SLED;

#endif
