#pragma once

#include <math.h>
#include <map>
#include "Extrapolator.h"
#include <Windows.h>

#pragma warning(disable: 4996)

struct Pos
{
	double x, y;
};

struct Packet
{
	double time;
	Pos pos;
};

class AnExtrapolator
{
public:
	AnExtrapolator(double x, double y);
	~AnExtrapolator();

	void UpdateTime();
	void DeliverPacket(Packet const &packet, double now);

private:
	AnExtrapolator();

	void InitRecordArray(double x, double y);
	int InitEPIC(double x, double y);
	void InitRealTime();
	double GetRealTime();
	double Latency();
	void SendPacket(Pos const &pos, double time);
	void ReceivePackets(double now);

	double lastReadTime = 0;
	double vel = 80.0f;
	double turn = 5;
	double dir = 0.0f;
	Pos myPos;
	double lastRecordedPos = 0;
	const static int RECORD_ARRAY_SIZE = 128;
	Pos recordArray[RECORD_ARRAY_SIZE];
	double recordTime[RECORD_ARRAY_SIZE];
	double lastSentTime;
	Pos extrapolatedPos[RECORD_ARRAY_SIZE];
	Extrapolator<2, double> intPos;
	bool gPointDisplay = false;
	bool gPaused = false;
	bool gStepping = false;
	double pauseDelta = 0;
	double lastPauseTime = 0;
	bool gRunning = true;
	bool gActive = true;
	bool keyDown[256];
	long long baseReading;
	long long lastRead;
	double tickMultiply;
	long long maxDelta;
	std::map<double, Packet> packetQueue;
};
