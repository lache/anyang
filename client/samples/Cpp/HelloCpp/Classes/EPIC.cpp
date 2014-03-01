// EPIC.cpp : Defines the entry point for the application.
//

#include "EPIC.h"
#include <math.h>
#include <map>
#include "Extrapolator.h"
#include <Windows.h>

#pragma warning(disable: 4996)

#define MAX_LOADSTRING 100

//  90 ms latency
#define LATENCY 0.25
//  30 ms jitter
#define JITTER 0.03
//  1/10 drop
#define DROPRATE 0.1
//  store drawing positions 20 times a second
#define STORERATE 20
//  send data packets 20 times a second
#define SENDRATE 1


double lastReadTime = 0;
struct Pos {
	float x, y;
};
float vel = 80.0f;
float turn = 5;
float dir = 0.0f;
float M_PI = 3.1415927f;
Pos myPos;
double lastRecordedPos = 0;
Pos recordArray[128];
double recordTime[128];

double lastSentTime;
Pos extrapolatedPos[128];
Extrapolator<2, float> intPos;

bool gPointDisplay = false;
bool gPaused = false;
bool gStepping = false;

double pauseDelta = 0;
double lastPauseTime = 0;

void UpdateTime();

bool gRunning = true;
bool gActive = true;
bool keyDown[256];

long long baseReading;
long long lastRead;
double tickMultiply;
long long maxDelta;

void InitRecordArray()
{
	myPos.x = 200;
	myPos.y = 150;
	intPos.Reset(0, 0, (float *)&myPos.x);
	for (int i = 0; i < 128; ++i) {
		recordArray[i] = myPos;
		recordTime[i] = 0;
		extrapolatedPos[i] = myPos;
	}
}

int InitEPIC()
{
	InitRealTime();
	InitRecordArray();
	timeBeginPeriod(3);

	//// Main message loop:
	//while (gRunning)
	//{
	//	if (PeekMessage(&msg, hWnd, 0, 0, PM_REMOVE))
	//	{
	//		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
	//		{
	//			TranslateMessage(&msg);
	//			DispatchMessage(&msg);
	//		}
	//	}
	//	else
	//	{
	//		Sleep(2);
	//		UpdateTime();
	//	}
	//}

	//timeEndPeriod(3);
	//return (int)msg.wParam;

	return 0;
}

void InitRealTime()
{
	long long tps;
	QueryPerformanceFrequency((LARGE_INTEGER *)&tps);
	tickMultiply = 1.0 / (double)tps;
	maxDelta = (long long)(tps * 0.1);
	QueryPerformanceCounter((LARGE_INTEGER *)&baseReading);
	lastRead = baseReading;
}

double GetRealTime()
{
	long long now;
	QueryPerformanceCounter((LARGE_INTEGER *)&now);
	//  work around dual-core bug
	if (now < lastRead) {
		now = lastRead + 1;
	}
	if (now - lastRead > maxDelta) {
		//  don't advance time too much all at once
		baseReading += now - lastRead - maxDelta;
	}
	lastRead = now;
	return (now - baseReading) * tickMultiply;
}

struct Packet {
	double time;
	Pos pos;
};

std::map<double, Packet> packetQueue;

double Latency()
{
	//  there might be some jitter!
	return LATENCY + (rand() & 0x7fff) / (32768.0 / (JITTER + 1e-6));
}

void SendPacket(Pos const &pos, double time)
{
	if ((rand() & 0x7fff) / 32768.0 < DROPRATE) {
		return;
	}
	Packet p;
	p.time = time;
	p.pos = pos;
	packetQueue[time + Latency()] = p;
}

void DeliverPacket(Packet const &packet, double now)
{
	intPos.AddSample(packet.time, now, &packet.pos.x);
}

void ReceivePackets(double now)
{
	while (packetQueue.size() > 0) {
		std::map<double, Packet>::iterator ptr = packetQueue.begin();
		if ((*ptr).first < now) {
			DeliverPacket((*ptr).second, now);
			packetQueue.erase(ptr);
		}
		else {
			break;
		}
	}
}

void UpdateTime()
{
	//RECT r;
	//r.bottom = r.left = r.right = r.top = 0;

	double now = GetRealTime();
	
	if (gPaused) {
		pauseDelta += (now - lastPauseTime);
		lastPauseTime = now;
		return;
	}

	now -= pauseDelta;
	ReceivePackets(now);
	double dt = now - lastReadTime;
	lastReadTime = now;
	
	if (dir < -M_PI) {
		dir += 2 * M_PI;
	}
	if (dir > M_PI) {
		dir -= 2 * M_PI;
	}
	myPos.x += float(cos(dir) * vel * dt);
	myPos.y -= float(sin(dir) * vel * dt);
	

	recordArray[0] = myPos;
	intPos.ReadPosition(now, &extrapolatedPos[0].x);
	if (now >= lastRecordedPos + 1.0 / STORERATE) {
		memmove(&extrapolatedPos[1], &extrapolatedPos[0], sizeof(extrapolatedPos)-sizeof(extrapolatedPos[0]));
		memmove(&recordArray[1], &recordArray[0], sizeof(recordArray)-sizeof(recordArray[0]));
		lastRecordedPos = now;
		if (gStepping) {
			gStepping = false;
			gPaused = true;
			lastPauseTime = GetRealTime();
			//::InvalidateRect(hWnd, 0, true);
		}
	}
	if (now >= lastSentTime + 1.0 / SENDRATE) {
		SendPacket(myPos, now);
		lastSentTime = now;
	}
}



#if !defined(NDEBUG)
class ExtrapolatorTest {
public:
	ExtrapolatorTest() {
		Extrapolator<1, float> i;
		float f = 0, p;
		i.Reset(0.1, 0.1, &f);
		assert(i.ReadPosition(0.1, &p));
		assert(p == 0);
		assert(!i.AddSample(0, 1, &p));
		f = 1;
		assert(i.AddSample(1, 1.5, &f));
		assert(i.EstimateLatency() < 0.5f);
		assert(i.EstimateLatency() > 0.1f);
		assert(i.EstimateUpdateTime() > 0.4f);
		assert(i.EstimateUpdateTime() < 1.0f);
		f = 2;
		assert(i.AddSample(1.5, 2, &f));
		assert(i.ReadPosition(2, &p));
		assert(fabs(2.5 - p) < 0.25);
		assert(i.EstimateLatency() < 0.5f);
		assert(i.EstimateLatency() > 0.3f);
		assert(i.EstimateUpdateTime() > 0.4f);
		assert(i.EstimateUpdateTime() < 0.6f);
		f = 3;
		assert(i.AddSample(2, 2.5, &f));
		assert(i.ReadPosition(2.5, &p));
		assert(fabs(4 - p) < 0.125);
		f = 4;
		assert(i.AddSample(2.5, 3, &f));
		assert(i.ReadPosition(3, &p));
		assert(fabs(5 - p) < 0.07);
		assert(i.ReadPosition(3.25, &p));
		assert(fabs(5.5 - p) < 0.07);
		//  don't allow extrapolation too far forward
		assert(!i.ReadPosition(4, &p));
	}
};

ExtrapolatorTest sTest;
#endif
