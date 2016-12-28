#ifndef _PULSE_H_
#define _PULSE_H_

#include"mcuhead.h"

#define Para     0.05

#define PWM_DIR_SET() SETBIT(GPIOB->ODR,BIT7)
#define PWM_DIR_CLR() CLRBIT(GPIOB->ODR,BIT7)

#define PWM_CW_SET() SETBIT(GPIOB->ODR,BIT6)
#define PWM_CW_CLR() CLRBIT(GPIOB->ODR,BIT6)


//=============================================================================================================================================================
//=============================================================================================================================================================

typedef enum
{
	OK=1,
	ERR,
}Err;
typedef enum
{
	addst,
	avest,
	redst,
	stop,
}st;
typedef enum
{
	Normal,
	Exception,
}mode;
typedef enum
{
	Forward,
	Reverse,
}FR;
typedef struct
{
	u32 FirstSpeed;//初速度
	u32 EndSpeed;	//终点速度
	u32 AddTime;//加速时间
	u32 MinusTime;//减速时间
	u32 Frequency;//频率
	s32 Quantity;//总需位移
	s32 JQu;		//绝对位移
	s32 NowQ;//当前位移
	u32 A;//加速度
	u32 D;//减速度
	u32 F;//当前频率
	u32 S;//当前时刻

	u32 Apcs;//加速脉冲数
	u32 Ddip;//减速脉冲数
	st ST;		//状态
	mode Mode;//模式
	FR fr;//正反转
}Pwm;
typedef struct
{
	u32 FirstSpeed;//初速度
	u32 EndSpeed;	//终点速度
	u32 AddTime;//加速时间
	u32 MinusTime;//减速时间
	u32 Frequency;//频率
	s32 Quantity;//总需位移	
}AD_Para;
typedef enum
{
	DRVI,
	DRVA,
}Pwm_Mode;
typedef enum
{
	OFF,
	ON,
}Pwm_Stast;
typedef struct
{
	u8 axis;
	AD_Para Para_Data;
	Pwm_Mode pwm_mode;
	Pwm_Stast pwm_stast;
}Axis;
//=============================================================================================================================================================
//=============================================================================================================================================================

s32 Send_JQuantity(u8 axis);

void JQuantity_Clear(u8 axis);

void Pwm_UpData(Axis* axis);
Err Pwm_Start(void);
Err Pwm_Stop(void);
u8 PWM_checkAllStop(void);

void PWM_AxisStats(u8 axis,u8 stats);



u8 DRVI3_UPDATA(Axis* axis);
u8 DRVA3_UPDATA(Axis* axis);



void TIM6_Init(u16 arr,u16 psc);


//=============================================================================================================================================================

void TIM4_Pulse(void);
Err AD_DRVI3(Pwm *pwmset);//相对定位函数
Err AD_DRVA3(Pwm* pwmset);//绝对定位函数

//=============================================================================================================================================================


#endif
