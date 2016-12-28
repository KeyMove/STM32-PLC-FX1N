#ifndef _encode_H_
#define _encode_H_

#include"mcuhead.h"

//PB9 BD
//PA7 BR
//PA6 BL

#define RL BIT6
#define RR BIT7
#define DD BIT9

typedef enum{
	RotL=0,
	RotR=1,
	RDown=2,
}ENCODETYPE;

typedef void(*EncodeCallBack)(u8);

typedef struct{
	void(*Init)(EncodeCallBack);
	void(*TimerCheck)(void);
}EnCodeBase;

extern const EnCodeBase encode;

#endif
