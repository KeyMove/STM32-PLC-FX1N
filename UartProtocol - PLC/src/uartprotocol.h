#ifndef _uartprotocol_h
#define _uartprotocol_h

#include"mcuhead.h"

//接收缓冲区大小
#define RECV_BUFF_LEN 64
//发送缓冲区大小
#define SEND_BUFF_LEN 128
//最大命令数量
#define MAX_CMD 16
//命令ID号
#define pubilc typedef

typedef enum
        {
            Alive = 0,
            SetOutputPort = 1,
            GetOutputPort = 2,
            GetInputPort = 3,
            SetPWMData=4,
            PWMStop=5,
            WriteData=6,
            LoadCodeData=7,
            ModeSwitch=8,
						ReadData=9,
        }PacketCmd;

//接收完成标志位
#define RF_OK 		1
//接收错误标志位
#define RF_ERROR 	2

//起始位置偏移
#define SEEK_BEGIN  0
//当前位置偏移
#define SEEK_OFFSET 1

#define LinkTime 2000
#define PacketTime 30




//------------------------------------------------------------------

typedef struct{
	void(*Seek)(s16,u8);
	//读取一个字节
	u8(*ReadByte)();
	//读取两个字节
	u16(*ReadWord)();
	//读取四个字节
	u32(*ReadDWord)();
	//读取指定字节
	u16(*ReadBuff)(u8* buff,u16 len);

	u8*(*GetBuff)();
	u16(*GetLen)();
	u8(*GetCmd)();
	u8(*WriteByte)(u8);
	u8(*WriteWord)(u16);
	u8(*WriteDWord)(u32);
	u16(*WriteBuff)(u8* buff,u16 len);
	u16(*WriteString)(u8*);
	u8*(*GetSendBuff)(void);
	void(*SendAckPacket)(void);
	void(*RelaySend)(void);
}UARTDATA;

typedef UARTDATA* UartEvent;
typedef void(*UartCallBack)(UartEvent);

typedef struct{
	//接收协议初始化
	//初始化所有接收相关的数据，并不会自动初始化串口
	//buff:接收发送缓冲区
	void(*Init)(u8* buff);
	
	//注册一个命令到一个函数
	//当收到这个命令的时候会自动调用该函数
	//函数类型为 void function(UartEvent e);
	//通过参数e即可访问收到的全部数据
	//cmd：要注册的命令ID function：函数
	void(*RegisterCmd)(u8 cmd,UartCallBack function);
	

	//注销一个函数
	//注销该函数所有命令
	//function：函数
	void(*unRegisterCmd)(UartCallBack function);
	

	//自动回应
	//命令回调函数后是否发送回应包
	//1：自动发送回应包 0：不自动发送回应包
	void(*AutoAck)(u8 stats);
	
	
	//发送回应包
	//发送指定ID号的回应包
	//如果设置了”自动回应“则执行回调函数执行完后自动发送一个回应包
	//cmd：回应的命令ID
	void(*SendCmdPacket)(u8 cmd);
	
	////发送带数据回应包
	////发送当前ID号的回应包
	////如果设置了”自动回应“这个会覆盖掉自动回应的数据内容
	////buff：数据内容 len：数据长度
	//void(*SendACKDataPacket)(u8 *buff,u16 len);
	//

	//发送数据包
	//可指定命令数据内容
	//cmd：命令id buff：数据内容 len：数据长度
	void(*SendPacket)(u8 cmd,u8* buff,u16 len);
	
	
	//检测是否存在数据包
	void(*Check)(void);
	
	//是否处于连接状态
	u8(*isLink)(void);
	
	u8* RecvBuff;
	u8* SendBuff;
	
}UartProtocolBase;

extern const UartProtocolBase UartProtocol;

void OnRecvData(u8 dat);
void TimeOutTick(void);

#endif
