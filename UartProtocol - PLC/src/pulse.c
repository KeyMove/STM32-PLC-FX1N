//=============================================================================================================================================================
//=============================================================================================================================================================
//=============================================================================================================================================================
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//=============================================================================================================================================================
//=============================================================================================================================================================
#include "pulse.h"
//#include "stm32f10x.h"                  // Device header
//#include "delay.h"
//#include "math.h"
#include "stdlib.h"
//#include "usart.h"
//#include "stm32f10x_tim.h"









static Axis Pwm3_Para;





static Pwm pwm3; 

//=============================================================================================================================================================
//=============================================================================================================================================================
//=============================================================================================================================================================

u8 PWM_checkAllStop(void)
{
	if(pwm3.ST == stop)
	{
		return 1;
	}
	return 0;
}

void PWM_AxisStats(u8 axis,u8 stats)
{

	Pwm3_Para.pwm_stast=(stats)?ON:OFF;

}
s32 Send_JQuantity(u8 axis)
{
	return pwm3.JQu;//if(pwm3.JQu >= 0)break;// return (pwm3.JQu - (pwm3.Quantity-pwm3.NowQ));else return (pwm3.JQu + (pwm3.Quantity-pwm3.NowQ)); break;
}
void JQuantity_Clear(u8 axis)
{
	pwm3.JQu=0;
}
void Pwm_UpData(Axis* axis)
{

		Pwm3_Para.axis = 2;
		Pwm3_Para.Para_Data.AddTime = axis->Para_Data.AddTime;
		Pwm3_Para.Para_Data.FirstSpeed = axis->Para_Data.FirstSpeed;
		Pwm3_Para.Para_Data.Frequency  = axis->Para_Data.Frequency;
		Pwm3_Para.Para_Data.EndSpeed = axis->Para_Data.FirstSpeed;//
		Pwm3_Para.Para_Data.MinusTime = axis->Para_Data.AddTime;//
		Pwm3_Para.Para_Data.Quantity  = axis->Para_Data.Quantity;
		Pwm3_Para.pwm_mode = axis->pwm_mode;
		Pwm3_Para.pwm_stast = axis->pwm_stast;
}

Err Pwm_Stop(void)
{
	TIM4->CR1 &= ~(1<<0);
	TIM3->CR1 &= ~(1<<0);
	return OK;
}
Err Pwm_Start(void)
{

	if(Pwm3_Para.pwm_stast == ON)
	{
		if(Pwm3_Para.pwm_mode == DRVI)
		{
			if(DRVI3_UPDATA(&Pwm3_Para)==1)return ERR;
		}else if(Pwm3_Para.pwm_mode == DRVA)
		{
			if(DRVA3_UPDATA(&Pwm3_Para)==1)return ERR;
		}
	}
	
	//delay_ms(20);
	
	if(Pwm3_Para.pwm_stast == ON)
	{
		pwm3.NowQ =0;
		
		pwm3.ST = addst;//加速状态

		pwm3.S         = 0;
		
		TIM4->CR1|=1<<0;              //启动定时器TIMER计数
		TIM3->CR1|=1<<0;			
	}	
	return OK;
}
	




//=============================================================================================================================================================
//=============================================================================================================================================================
//=============================================================================================================================================================

void TIM6_Init(u16 arr,u16 psc)
{
//	TIM_TimeBaseInitTypeDef  TIM_TimeBaseStructure;
//	NVIC_InitTypeDef NVIC_InitStructure;
//	
//	RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM3, ENABLE); //时钟使能
//	
//	TIM_TimeBaseStructure.TIM_Period = arr; //设置在下一个更新事件装入活动的自动重装载寄存器周期的值	
//	TIM_TimeBaseStructure.TIM_Prescaler =psc; //设置用来作为TIMx时钟频率除数的预分频值
//	TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1; //设置时钟分割:TDTS = Tck_tim
//	TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up;  //TIM向上计数模式
//	TIM_TimeBaseInit(TIM3, &TIM_TimeBaseStructure); //根据指定的参数初始化TIMx的时间基数单位  
// 
//	TIM_ITConfig(TIM3,TIM_IT_Update,ENABLE ); //使能指定的TIM6中断,允许更新中断
//	
//	//中断优先级NVIC设置
//	NVIC_InitStructure.NVIC_IRQChannel = TIM3_IRQn;  //TIM3中断
//	NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 0;  //先占优先级0级
//	NVIC_InitStructure.NVIC_IRQChannelSubPriority = 0;  //从优先级3级
//	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE; //IRQ通道被使能
//	NVIC_Init(&NVIC_InitStructure);  //初始化NVIC寄存器
	RCC->APB1ENR|=RCC_APB1ENR_TIM3EN;
	
	TIM3->ARR=arr;
	TIM3->PSC=psc;

	TIM3->DIER |= TIM_DIER_UIE;
	
	NVIC_SetPriority(TIM3_IRQn,2);
	NVIC_EnableIRQ(TIM3_IRQn);

	//TIM_Cmd(TIM3, ENABLE);  //使能TIMx	
	//TIM6->CR1 |= 1<<0;
	TIM3->CR1 &= ~(1<<0);
}


//定时器6中断服务程序
void TIM3_IRQHandler(void)   //TIM3中断
{
	if(TIM3->SR&0x0001)
	{
		if(pwm3.ST != stop)
		{
			if(pwm3.ST == addst)
			{
				pwm3.S ++;
			}else if(pwm3.ST == redst && pwm3.S > 0)
			{
				pwm3.S --;
			}				
		}
		if(pwm3.ST == stop)
		{
			TIM3->CR1 &= ~(1<<0);
		}
		TIM3->SR = 0x0000;
	}
}


//=============================================================================================================================================================
//=============================================================================================================================================================
//=============================================================================================================================================================


void TIM4_Pulse(void)
{
	
	RCC->APB1ENR|=RCC_APB1ENR_TIM4EN;
	RCC->APB2ENR|=RCC_APB2ENR_IOPBEN|RCC_APB2ENR_AFIOEN;
	
	GPIOB->CRL&=0x00ffffff;
	GPIOB->CRL|=0x3b000000;
	
	//定时器TIM3初始化
	TIM4->ARR = 0; //设置在下一个更新事件装入活动的自动重装载寄存器周期的值	
	TIM4->PSC = 72-1;                  //设置定时器2预分频值，使定时器得到1MHz的计数频率
	TIM4->CR1 |= 1<<2;              //设置只有计数溢出作为更新中断
	TIM4->DIER |= 1<<0;  //允许定时器2计数溢出中断
	
	TIM4->CCMR1 &= ~(3<<0);  //CC1通道配置为输出模式
	TIM4->CCMR1 |= 7<<4;  //输出比较1为PWM模式2
	TIM4->CCER  |= 1<<0;    //通道1输出使能		
	
	NVIC_SetPriority(TIM4_IRQn,0);
	NVIC_EnableIRQ(TIM4_IRQn);
	
	pwm3.JQu=0;
	pwm3.ST = stop;
		
}
void TIM4_IRQHandler()                    //定时器2全局中断函数
{
	if (TIM4->SR&0x0001)  //检查TIM3更新中断发生与否
	{
		pwm3.NowQ ++;
		if(pwm3.ST == addst)//加速状态
		{
			pwm3.F = pwm3.FirstSpeed+((pwm3.A*pwm3.S)/1000);
			TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
			TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%

			if(pwm3.NowQ == pwm3.Apcs ) 
			{
				pwm3.ST = avest;
			}
			
		}else if(pwm3.ST == avest)//均速状态
		{
			if(pwm3.NowQ >= pwm3.Ddip) 
			{	
				pwm3.ST = redst;
				if(pwm3.Mode == Normal)//
				{
					pwm3.S = pwm3.MinusTime;//
				}
			}
		}else if(pwm3.ST == redst)//减速状态
		{
			pwm3.F = pwm3.EndSpeed+((pwm3.D*pwm3.S)/1000);
			TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
			TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%
		}
		if(pwm3.NowQ == pwm3.Quantity)
		{
			TIM4->CR1 &= ~(1<<0);
			//TIM6->CR1 &= ~(1<<0);
			pwm3.ST = stop;
		}
		TIM4->SR = 0x0000;  //清除TIMx更新中断标志 
	}
}
Err AD_DRVI3(Pwm* pwmset)
{
	if(pwmset ->Quantity==0) return ERR; 
	if(pwmset->FirstSpeed < 20 ) pwmset->FirstSpeed = 20;
	if(pwmset->EndSpeed   < 20 ) pwmset->EndSpeed   = 20;
	if(pwmset->Frequency  < pwmset->FirstSpeed) pwmset->Frequency = pwmset->FirstSpeed;
	if(pwmset->EndSpeed   > pwmset->Frequency)  pwmset->EndSpeed  = pwmset ->Frequency;

	pwm3.JQu   	   = pwm3.JQu + pwmset->Quantity;
	
	if(pwmset->Quantity>0)
	{
		PWM_DIR_SET();
	}else if(pwmset->Quantity<0)
	{
		PWM_DIR_CLR();
	}
	
	pwm3.Quantity  = abs(pwmset->Quantity);			//数量
	
	pwm3.Frequency = pwmset->Frequency;				//频率
	pwm3.FirstSpeed = pwmset->FirstSpeed;			//初始频率
	pwm3.EndSpeed   = pwmset->EndSpeed;				//结束频率
	pwm3.AddTime = pwmset->AddTime;					//加速时间
	pwm3.MinusTime = pwmset->MinusTime;				//减速时间
	pwm3.A         = (pwm3.Frequency-pwm3.FirstSpeed)/((float)pwm3.AddTime/1000);//拿到加速度
	pwm3.D         = (pwm3.Frequency-pwm3.EndSpeed)/((float)pwm3.MinusTime/1000);//拿到减速度
	pwm3.Apcs      = ((pwm3.Frequency*((float)pwm3.AddTime/1000))-(((pwm3.Frequency-pwm3.FirstSpeed)*((float)pwm3.AddTime/1000))/2));//拿到加速pcs
	pwm3.Ddip      = ((pwm3.Frequency*((float)pwm3.MinusTime/1000))-(((pwm3.Frequency-pwm3.EndSpeed)*((float)pwm3.MinusTime/1000))/2));//拿到减速pcs
	pwm3.Ddip      = pwm3.Quantity-pwm3.Ddip;
	
	if(pwm3.Apcs > pwm3.Ddip) 
	{
		pwm3.Mode = Exception;
		pwm3.Apcs = pwm3.Ddip ;//中断加速,进入减速
	}else 
	{
		pwm3.Mode = Normal;
	}
	
	
	pwm3.F         = pwm3.FirstSpeed;//当前频率
	
	TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
	TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%
	

		
	
//	delay_ms(10);
	
	pwm3.NowQ =0;
	
	pwm3.ST = addst;//加速状态
	
	pwm3.S         = 0;

	TIM4->CR1|=1<<0;              //启动定时器TIMER3计数
	TIM3->CR1|=1<<0;	
	
	return OK;
}
Err AD_DRVA3(Pwm* pwmset)
{
	if(pwmset->FirstSpeed < 20 ) pwmset->FirstSpeed = 20;
	if(pwmset->EndSpeed   < 20 ) pwmset->EndSpeed   = 20;
	if(pwmset->Frequency  < pwmset->FirstSpeed) pwmset->Frequency = pwmset->FirstSpeed;
	if(pwmset->EndSpeed   > pwmset->Frequency)  pwmset->EndSpeed  = pwmset ->Frequency;

	if(pwm3.JQu > pwmset->Quantity)
	{
		PWM_DIR_CLR();
		pwm3.Quantity = abs(pwm3.JQu - pwmset->Quantity);
	}else if(pwm3.JQu < pwmset->Quantity)
	{
		PWM_DIR_SET();
		pwm3.Quantity = abs(pwmset->Quantity - pwm3.JQu) ;
	}else if(pwm3.JQu == pwmset->Quantity)
	{
		return ERR;
	}
	pwm3.JQu = pwmset->Quantity;
	
	pwm3.Frequency  = pwmset->Frequency;				//频率
	pwm3.FirstSpeed = pwmset->FirstSpeed;			//初始频率
	pwm3.EndSpeed   = pwmset->EndSpeed;				//结束频率
	pwm3.AddTime    = pwmset->AddTime;					//加速时间
	pwm3.MinusTime  = pwmset->MinusTime;				//减速时间
	pwm3.A          = (pwm3.Frequency-pwm3.FirstSpeed)/((float)pwm3.AddTime/1000);//拿到加速度
	pwm3.D          = (pwm3.Frequency-pwm3.EndSpeed)/((float)pwm3.MinusTime/1000);//拿到减速度

	pwm3.Apcs       = ((pwm3.Frequency*((float)pwm3.AddTime/1000))-(((pwm3.Frequency-pwm3.FirstSpeed)*((float)pwm3.AddTime/1000))/2));//拿到加速pcs
	pwm3.Ddip       = ((pwm3.Frequency*((float)pwm3.MinusTime/1000))-(((pwm3.Frequency-pwm3.EndSpeed)*((float)pwm3.MinusTime/1000))/2));//拿到减速pcs
	pwm3.Ddip       = pwm3.Quantity-pwm3.Ddip;
	if(pwm3.Apcs > pwm3.Ddip) 
	{
		pwm3.Mode = Exception;
		pwm3.Apcs = pwm3.Ddip ;//中断加速,进入减速
	}else 
	{
		pwm3.Mode = Normal;
	}
	
	pwm3.F          = pwm3.FirstSpeed;//当前频率
	
	TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
	TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%
	
//	delay_ms(10);
	
	pwm3.ST   = addst;//加速状态
	
	pwm3.NowQ = 0;

	pwm3.S    = 0;
	
	TIM4->CR1|=1<<0;              //启动定时器TIMER计数
	TIM3->CR1|=1<<0;	
	
	return OK;
}
u8 DRVI3_UPDATA(Axis* axis)
{
	if(axis->Para_Data.Quantity==0) return 1; 
	if(axis->Para_Data.FirstSpeed < 20 ) axis->Para_Data.FirstSpeed = 20;
	if(axis->Para_Data.EndSpeed   < 20 ) axis->Para_Data.EndSpeed   = 20;
	if(axis->Para_Data.Frequency  < axis->Para_Data.FirstSpeed) axis->Para_Data.Frequency  = axis->Para_Data.FirstSpeed;
	if(axis->Para_Data.EndSpeed   > axis->Para_Data.Frequency)  axis->Para_Data.EndSpeed   = axis->Para_Data.Frequency;
	
	pwm3.JQu   	   = pwm3.JQu + axis->Para_Data.Quantity;

	if(axis->Para_Data.Quantity>0)
	{
		PWM_DIR_SET();
	}else if(axis->Para_Data.Quantity<0)
	{
		PWM_DIR_CLR();
	}
	
	pwm3.Quantity  = abs(axis->Para_Data.Quantity);				//数量
	
	pwm3.Frequency = axis->Para_Data.Frequency;				//频率
	pwm3.FirstSpeed = axis->Para_Data.FirstSpeed;			//初始频率
	pwm3.EndSpeed   = axis->Para_Data.EndSpeed;				//结束频率
	pwm3.AddTime = axis->Para_Data.AddTime;					//加速时间
	pwm3.MinusTime = axis->Para_Data.MinusTime;				//减速时间
	pwm3.A         = (pwm3.Frequency-pwm3.FirstSpeed)/((float)pwm3.AddTime/1000);//拿到加速度
	pwm3.D         = (pwm3.Frequency-pwm3.EndSpeed)/((float)pwm3.MinusTime/1000);//拿到减速度

	pwm3.Apcs      = ((pwm3.Frequency*((float)pwm3.AddTime/1000))-(((pwm3.Frequency-pwm3.FirstSpeed)*((float)pwm3.AddTime/1000))/2));//拿到加速pcs
	pwm3.Ddip      = ((pwm3.Frequency*((float)pwm3.MinusTime/1000))-(((pwm3.Frequency-pwm3.EndSpeed)*((float)pwm3.MinusTime/1000))/2));//拿到减速pcs
	pwm3.Ddip      = pwm3.Quantity-pwm3.Ddip;
	if(pwm3.Apcs > pwm3.Ddip) 
	{
		pwm3.Mode = Exception;//
		pwm3.Apcs = pwm3.Ddip ;//中断加速,进入减速
	}else 
	{
		pwm3.Mode = Normal;//
	}
		
	pwm3.F         = pwm3.FirstSpeed;//当前频率
	
	TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
	TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%
	
	return 0;
}
u8 DRVA3_UPDATA(Axis* axis)
{
	if(axis->Para_Data.FirstSpeed< 20 ) axis->Para_Data.FirstSpeed = 20;
	if(axis->Para_Data.EndSpeed   < 20 ) axis->Para_Data.EndSpeed   = 20;
	if(axis->Para_Data.Frequency  < axis->Para_Data.FirstSpeed) axis->Para_Data.Frequency  = axis->Para_Data.FirstSpeed;
	if(axis->Para_Data.EndSpeed   > axis->Para_Data.Frequency)  axis->Para_Data.EndSpeed   = axis->Para_Data.Frequency;

	if(pwm3.JQu > axis->Para_Data.Quantity)
	{
		PWM_DIR_CLR();
		pwm3.Quantity = abs(pwm3.JQu - axis->Para_Data.Quantity);
	}else if(pwm3.JQu < axis->Para_Data.Quantity)
	{
		PWM_DIR_SET();
		pwm3.Quantity = abs(axis->Para_Data.Quantity - pwm3.JQu);
	}else if(pwm3.JQu == axis->Para_Data.Quantity)
	{
		return 1;
	}
	pwm3.JQu = axis->Para_Data.Quantity;
	
	pwm3.Frequency = axis->Para_Data.Frequency;				//频率
	pwm3.FirstSpeed = axis->Para_Data.FirstSpeed;			//初始频率
	pwm3.EndSpeed   = axis->Para_Data.EndSpeed;				//结束频率
	pwm3.AddTime = axis->Para_Data.AddTime;					//加速时间
	pwm3.MinusTime = axis->Para_Data.MinusTime;				//减速时间
	pwm3.A         = (pwm3.Frequency-pwm3.FirstSpeed)/((float)pwm3.AddTime/1000);//拿到加速度
	pwm3.D         = (pwm3.Frequency-pwm3.EndSpeed)/((float)pwm3.MinusTime/1000);//拿到减速度

	pwm3.Apcs      = ((pwm3.Frequency*((float)pwm3.AddTime/1000))-(((pwm3.Frequency-pwm3.FirstSpeed)*((float)pwm3.AddTime/1000))/2));//拿到加速pcs
	pwm3.Ddip      = ((pwm3.Frequency*((float)pwm3.MinusTime/1000))-(((pwm3.Frequency-pwm3.EndSpeed)*((float)pwm3.MinusTime/1000))/2));//拿到减速pcs
	pwm3.Ddip      = pwm3.Quantity-pwm3.Ddip;
	if(pwm3.Apcs > pwm3.Ddip) 
	{
		pwm3.Mode = Exception;
		pwm3.Apcs = pwm3.Ddip ;//中断加速,进入减速
	}else 
	{
		pwm3.Mode = Normal;
	}
		
	pwm3.F         = pwm3.FirstSpeed;//当前频率
	
	TIM4->ARR=1000000/pwm3.F-1;  //设定重装值
	TIM4->CCR1=TIM4->ARR>>1;   //匹配值1等于重装值一半，是以占空比为50%
	
	return 0;
}


