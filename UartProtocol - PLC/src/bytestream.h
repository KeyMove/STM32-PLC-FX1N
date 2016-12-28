#ifndef _bytestream_H_
#define _bytestream_H_

#include"mcuhead.h"

typedef struct{
	u8(*isReadEmpty)(void);
	u8(*ReadByte)(void);
	u16(*ReadWord)(void);
	u32(*ReadDWord)(void);
	u16(*ReadBuff)(u8* buff,u16 len);
	u16(*ReadString)(u8* buff);
	void(*ReadSeek)(u8 type, s16 seek);
	void(*SetReadBuff)(u8* buff,u16 len);
	u8*(*GetReadBuff)(void);
	void(*WriteByte)(u8);
	void(*WriteWord)(u16);
	void(*WriteDWord)(u32);
	u16(*WriteBuff)(u8* buff,u16 len);
	u16(*WriteString)(u8* str);
	void(*SetWriteBuff)(u8* buff,u16 len);
	u8*(*GetWriteBuff)(void);
}ByteStreamBase;

extern const ByteStreamBase ByteStream;

#endif
