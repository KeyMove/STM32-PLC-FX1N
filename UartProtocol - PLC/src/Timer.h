#ifndef Timer_H
#define Timer_H

//Build for STM32 Cortex M0 2014年11月5日16:01:09 @KeyMove

#include"mcuhead.h"

#define u8 unsigned char
#define u16 unsigned short
#define u32 unsigned long

typedef void(*TimerCallBack)(void);

typedef struct{
	u16 time;
	u16 nTime;
	TimerCallBack fun;
	union{
	u32 bAddr;
	u8 cAddr[4];
	}breakAddr;
}_Timer;

#define MAXTIMER 8			//最大定时器数量

#define TimerBit u8			//根据定时器数量选择,小于8个选u8,大于8个小于16个选u16,大于16个选u32,最大同时支持32个定时器

void TimerRun(void);
void KillTimer(u8 id);
void KillThisTimer(void);
u8 SetTimer(u16 time,TimerCallBack fun);
void InitTimer(u8);
void Delay(u16 time);
void SetDelay(u16 time);
void AddDelay(u16 time);
u8 isUse(u8 id);
void ZeroDelay(void);

void ZeroTargetDelay(u8 id);
void AddTargetDelay(u8 id, u16 time);
void SetTargetDelay(u8 id, u16 time);

typedef struct{
	void(*Init)(u8);//初始化
	void(*Run)();//运行
	void(*Stop)(u8);
	void(*StopThis)();
	u8(*isUse)(u8);
	u8(*Start)(u16,TimerCallBack);
	void(*Delay)(u16);
	struct{
		void(*add)(u16);
		void(*set)(u16);
		void(*ready)();
	}Tick;
	struct {
		void(*add)(u8,u16);
		void(*set)(u8,u16);
		void(*ready)(u8);
	}Target;
}TimerBase;

extern const TimerBase Timer;

#endif

