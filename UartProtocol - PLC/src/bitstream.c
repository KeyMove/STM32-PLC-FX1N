#include"bitstream.h"

static u16 read(u8* buff, u16 len) {
	
}

static u16 write(u8* buff, u16 len) {

}

static void seek(u16 len) {

}

static u16 setoutput(u8* buff, u16 len) {

}

static u16 setinput(u8* buff, u16 len) {

}

const BitStreamBase BitStream = {read,write,seek,setinput,setoutput};