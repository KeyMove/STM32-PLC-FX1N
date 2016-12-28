#include"uart.H"

#define DMA

const UARTBase UART={
	UART_Init,
	UART_SendByte,
	UART_SendString,
	UART_SendBuff,
	UART_ReSetBps,
	UART_OnRecv
};

void (*OnRecv)(u8);
static u32 f;
void UART_Init(u8 f_mhz,u32 b,UARTCALLBACK fun){
	f=f_mhz*1000000;
	OnRecv=fun;
	//RCC->APB2ENR=RCC_APB2ENR_USART1EN;
	RCC->APB2ENR |= RCC_APB2ENR_IOPAEN | RCC_APB2ENR_USART1EN;
	USART1->CR1=USART1->CR2=USART1->CR3=0;
#ifdef DMA
	RCC->AHBENR |= RCC_AHBENR_DMA1EN;
	USART1->CR3 |= USART_CR3_DMAT;
	DMA1_Channel4->CCR = DMA_CCR4_MINC|DMA_CCR4_DIR;
	DMA1_Channel4->CPAR = (u32)&USART1->DR;
#endif
	GPIOA->CRH&=0xfffff00f;
	GPIOA->CRH|=0x000008b0;
	GPIOA->ODR|=GPIO_ODR_ODR10|GPIO_ODR_ODR9;
	USART1->CR1=USART_CR1_RE|USART_CR1_TE|USART_CR1_RXNEIE;
	NVIC_SetPriority(USART1_IRQn,2);
	NVIC_EnableIRQ(USART1_IRQn);
	UART_ReSetBps(b);
}

void UART_ReSetBps(u32 b)
{
	u32 baud=(25*f)/(4*b);
	u32 t=(baud/100)<<4;
	u32 t2=baud-(100*(t>>4));
	USART1->CR1&=~USART_CR1_UE;
	USART1->BRR=t|((((t2*16)+50)/100)&0x0f);
	USART1->CR1|=USART_CR1_UE;
}

void UART_SetEnable(u8 set){
	
}

void UART_SendByte(u8 dat){
	USART1->DR=dat;
	while(!(USART1->SR&USART_SR_TC));
}

void UART_SendBuff(u8 *buff,u16 len) {
	if(len==0)return;
#ifdef DMA
	if(DMA1_Channel4->CCR&DMA_CCR1_EN)
	{
		while (!(DMA1->ISR&DMA_ISR_TCIF4));
		while(!(USART1->SR&USART_SR_TC));
	}
	SETBIT(DMA1->IFCR, DMA_IFCR_CTCIF4);
	CLRBIT(DMA1_Channel4->CCR, DMA_CCR1_EN);
	DMA1_Channel4->CMAR = (u32)buff;
	DMA1_Channel4->CNDTR = len;
	SETBIT(DMA1_Channel4->CCR, DMA_CCR1_EN);
#else
	while(len--)
		UART_SendByte(*buff++);
#endif
}

void UART_SendString(u8* p) {
	u8* s=p;
	while (*p++);
	//while (*p)
		//UART_SendByte(*p++);
	UART_SendBuff(s, p-s-1);
}

void USART1_IRQHandler(void)
{
	u8 dat=USART1->DR;
	USART1->SR;
	OnRecv(dat);
}

void UART_OnRecv(void(*p)(u8)){
	OnRecv=p;
}
