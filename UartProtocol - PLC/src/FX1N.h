#ifndef _FX1N_H_
#define _FX1N_H_

#include "mcuhead.h"

#define S_Point 1000
#define X_Point 128
#define Y_Point 128
#define T_Point 32
#define M_Point 1536
#define C_Point 32
#define MX_Point 12
#define D_Point 32

#define POINTSIZE(x) ((x+7)/8)
#define FX1N_BUFFSIZE ((POINTSIZE(S_Point)+POINTSIZE(X_Point)+POINTSIZE(Y_Point)+POINTSIZE(T_Point)+POINTSIZE(M_Point)+POINTSIZE(C_Point)+POINTSIZE(MX_Point)+D_Point*sizeof(u16)+T_Point*sizeof(u16)+C_Point*sizeof(u16))*2)

typedef void(*IOCallBack)(u8,u8*);

typedef struct {
	void(*Init)(u8*);
	u8(*LoadCode)(u8*);
	void(*Reset)(void);
	void(*SetIOCallBack)(IOCallBack ReadIO,IOCallBack WriteIO);
	
	void(*CodeLoop)();
	void(*UpdateTimer)(void);
}FX1NBase;

extern const FX1NBase FX1N;

#endif
