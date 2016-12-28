#include"flash.h"
//#include <absacc.h>

#define KEY1 0x45670123
#define KEY2 0xCDEF89AB

#define FLASH_ADDR 0x8008000
#define FLASH_SIZE 10240

#define SEC_SIZE 1024

static const u8 codetable[FLASH_SIZE] __attribute__ ((at(FLASH_ADDR))) = {0};

static void FlashUnLock(void){
	FLASH->KEYR = KEY1;
	FLASH->KEYR = KEY2;
}

static void FlashLock(void) {
	FLASH->CR |= FLASH_CR_LOCK;
}

static void FlashErase(u32 page) {
	u16 i;
	for(i=0;i<SEC_SIZE;i++)
	{
		if(*((__IO u8*)page+i)!=0xff)
		{
			FLASH->CR |= FLASH_CR_PER; 
			FLASH->AR = page;
			FLASH->CR |= FLASH_CR_STRT;
			while (FLASH->SR&(FLASH_SR_BSY | FLASH_SR_PGERR | FLASH_SR_WRPRTERR));
			CLRBIT(FLASH->CR, FLASH_CR_PER);
			return;
		}
	}
	//while (FLASH->SR&(FLASH_SR_BSY | FLASH_SR_PGERR | FLASH_SR_WRPRTERR));
	
}

static void FlashWaitBusy(void) {
	while (FLASH->SR&(FLASH_SR_BSY | FLASH_SR_PGERR | FLASH_SR_WRPRTERR));
}

static void FlashWrite(u32 addr,u8* ptr,u16 len)
{
	u16 i;
	u32 data;
	u16 data16;
	FlashUnLock();
	SETBIT(FLASH->CR, FLASH_CR_PG);
	while(len>=SEC_SIZE)
	{
		FlashErase(addr);
		for(i=0;i<SEC_SIZE/4;i++)
		{
			_32T8LL(data) = *ptr++;
			_32T8L(data) = *ptr++;
			_32T8H(data) = *ptr++;
			_32T8HH(data) = *ptr++;
			*(__IO u32*)addr=data;
			addr += 4;
		}
		len-=SEC_SIZE;
	}
	if(len)
	{
		for(i=0;i<len/2;i++)
		{
			_16T8L(data16) = *ptr++;
			_16T8H(data16) = *ptr++;
			*(__IO u16*)addr=data16;
			addr += 2;
		}
		if(len&1)
			*(__IO u16*)addr = ((*ptr));
	}
	CLRBIT(FLASH->CR, FLASH_CR_PG);
	FlashLock();
}

const FlashBase Flash = {
	FlashUnLock,
	FlashLock,
	FlashWaitBusy,
	FlashErase,
	FlashWrite,
	codetable,
};

