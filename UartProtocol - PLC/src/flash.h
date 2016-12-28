#ifndef _flash_H_
#define _flash_H_

#include"mcuhead.h"

typedef struct
{
	void(*Unlock)(void);
	void(*Lock)(void);
	void(*WaitBusy)(void);
	void(*Erase)(u32 addr);
	void(*WriteData)(u32 Addr, u8* dat, u16 len);
	const u8* Data;
}FlashBase;

extern const FlashBase Flash;

#endif
