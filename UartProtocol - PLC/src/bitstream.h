#ifndef _bitstream_H_
#define _bitstream_H_

#include"mcuhead.h"

typedef struct {
	u16(*Read)(u8 *buff, u16 bitlen);
	u16(*Write)(u8 *buff, u16 bitlen);
	void(*Seek)(u16 pos);
	void(*SetReadBuff)(u8*buff, u16 len);
	void(*SetWriteBuff)(u8*buff, u16 len);
}BitStreamBase;

extern BitStreamBase BitStream;

#endif
