#ifndef _mcuhead_H_
#define _mcuhead_H_

#include "stm32f10x.h"

#define u8 unsigned char
#define u16 unsigned short
#define u32 unsigned long

#define s8 signed char
#define s16 signed short
#define s32 signed long

#define vu8 volatile u8
#define vu16 volatile u16
#define vu32 volatile u32


#define BIT0 1
#define BIT1 2
#define BIT2 4
#define BIT3 8
#define BIT4 0x10
#define BIT5 0x20
#define BIT6 0x40
#define BIT7 0x80
#define BIT8 0x100
#define BIT9 0x200
#define BIT10 0x400
#define BIT11 0x800
#define BIT12 0x1000
#define BIT13 0x2000
#define BIT14 0x4000
#define BIT15 0x8000
#define BIT16 0x10000
#define BIT17 0x20000
#define BIT18 0x40000
#define BIT19 0x80000
#define BIT20 0x100000
#define BIT21 0x200000
#define BIT22 0x400000
#define BIT23 0x800000
#define BIT24 0x1000000
#define BIT25 0x2000000
#define BIT26 0x4000000
#define BIT27 0x8000000
#define BIT28 0x10000000
#define BIT29 0x20000000
#define BIT30 0x40000000
#define BIT31 0x80000000


#define SETBIT(x,y) (x)|=(y)
#define CLRBIT(x,y) (x)&=~(y)
#define CPLBIT(x,y) (x)^=(y);

union u2i{
	vu32 l;
	vu16 li[2];
	vu8 lc[4];
	vu16 i;
	vu8 c[2];
};

#define _16T8H(x) (((union u2i*)&x)->c[1])//16位转高8位
#define _16T8L(x) (((union u2i*)&x)->c[0])//??16??8?

#define _32T16H(x) (((union u2i*)&x)->li[1])//??32??16?
#define _32T16L(x) (((union u2i*)&x)->li[0])//??32??16?

#define _32T8HH(x) (((union u2i*)&x)->lc[3])//??32??8?
#define _32T8H(x) (((union u2i*)&x)->lc[2])//??32???8?
#define _32T8L(x) (((union u2i*)&x)->lc[1])//??32???8?
#define _32T8LL(x) (((union u2i*)&x)->lc[0])//??32?8?

#define BIT(x) (((x>>21)&BIT7)|((x>>18)&BIT6)|((x>>15)&BIT5)|((x>>12)&BIT4)|((x>>9)&BIT3)|((x>>6)&BIT2)|((x>>3)&BIT1)|((x)&BIT0))
#define BIN(X) BIT(0x##X)
#undef BIT

#define BINBYTE(b) BIM(b)
#define BINWORD(b1,b2) ((BIN(b1)<<8)|BIN(b2))
#define BINDWORD(b1,b2,b3,b4) ((BIN(b1)<<24)|(BIN(b2)<<16)|(BIN(b3)<<8)|BIN(b4))

#define _0xf   (u32)0xf
#define PPMODE (u32)0x3
#define PUMODE (u32)0x8
#define AFMODE (u32)0xb

#define HT4(x,y,z) (((x&(1<<y))!=0)*(z<<4*y))
#define HT16(x) (~(HT4(x,0,_0xf)|HT4(x,1,_0xf)|HT4(x,2,_0xf)|HT4(x,3,_0xf)|HT4(x,4,_0xf)|HT4(x,5,_0xf)|HT4(x,6,_0xf)|HT4(x,7,_0xf)))

#define PP16(x) (HT4(x,0,PPMODE)|HT4(x,1,PPMODE)|HT4(x,2,PPMODE)|HT4(x,3,PPMODE)|HT4(x,4,PPMODE)|HT4(x,5,PPMODE)|HT4(x,6,PPMODE)|HT4(x,7,PPMODE))
#define _GPIO_PP(io,b,b2) io->CRH=(io->CRH&HT16(b1))|PP16(b1);io->CRL=(io->CRL&HT16(b2))|PP16(b2);
#define GPIO_PP(io,b1,b2) _GPIO_PP(io,BIN(b1),BIN(b2))

#define PU16(x) (HT4(x,0,PUMODE)|HT4(x,1,PUMODE)|HT4(x,2,PUMODE)|HT4(x,3,PUMODE)|HT4(x,4,PUMODE)|HT4(x,5,PUMODE)|HT4(x,6,PUMODE)|HT4(x,7,PUMODE))
#define _GPIO_PU(io,b1,b2) io->CRH=(io->CRH&HT16(b1))|PU16(b1);io->CRL=(io->CRL&HT16(b2))|PU16(b2);
#define GPIO_PU(io,b1,b2) _GPIO_PU(io,BIN(b1),BIN(b2))

#define AF16(x) (HT4(x,0,AFMODE)|HT4(x,1,AFMODE)|HT4(x,2,AFMODE)|HT4(x,3,AFMODE)|HT4(x,4,AFMODE)|HT4(x,5,AFMODE)|HT4(x,6,AFMODE)|HT4(x,7,AFMODE))
#define _GPIO_AF(io,b1,b2) io->CRH=(io->CRH&HT16(b1))|AF16(b1);io->CRL=(io->CRL&HT16(b2))|AF16(b2);
#define GPIO_AF(io,b1,b2) _GPIO_AF(io,BIN(b1),BIN(b2))

#endif
