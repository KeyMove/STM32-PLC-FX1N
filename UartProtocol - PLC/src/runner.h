#ifndef _runner_H_
#define _runner_H_

#include"mcuhead.h"
#include"uartprotocol.h"

//u8 LoadCode(void);
void RunCode(void);
void StopCode(void);

extern u16 DelayTime;

//输出端口的数量
#define OUTPUTCOUNT 12
//输入端口的数量
#define INPUTCOUNT 20
//PWM端口的数量
#define PWMCOUNT 1
//设备ID
#define ID 0xff50


extern u8 InputData[32];
extern u8 OutputData[32];
extern u8 lastCheckIO;
extern u16 PC;
typedef struct
{
	void(*SetOutput)(UartEvent e);
	void(*GetInput)(UartEvent e);
	u8(*LoadCode)(u8);
	void(*RunCode)();
	void(*StopCode)();
	u8*(*GetCode)(u8 index);
	u16(*GetCodeSize)(u8 index);
}RunnerBase;

extern const RunnerBase CodeRunner;

#endif
