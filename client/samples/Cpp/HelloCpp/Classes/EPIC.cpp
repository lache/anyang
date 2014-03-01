#include "EPIC.h"

const static double M_PI = 3.1415927f;

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

AnExtrapolator::AnExtrapolator(double x, double y)
{
	InitEPIC(x, y);
}

AnExtrapolator::~AnExtrapolator()
{

}

void AnExtrapolator::InitRecordArray(double x, double y)
{
	myPos.x = x;
	myPos.y = y;
	intPos.Reset(0, 0, (double *)&myPos.x);
	for (int i = 0; i < RECORD_ARRAY_SIZE; ++i) {
		recordArray[i] = myPos;
		recordTime[i] = 0;
		extrapolatedPos[i] = myPos;
	}
}

int AnExtrapolator::InitEPIC(double x, double y)
{
	InitRealTime();
	InitRecordArray(x, y);
	timeBeginPeriod(3);

	return 0;
}

void AnExtrapolator::InitRealTime()
{
	long long tps;
	QueryPerformanceFrequency((LARGE_INTEGER *)&tps);
	tickMultiply = 1.0 / (double)tps;
	maxDelta = (long long)(tps * 0.1);
	QueryPerformanceCounter((LARGE_INTEGER *)&baseReading);
	lastRead = baseReading;
}

double AnExtrapolator::GetRealTime()
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

double AnExtrapolator::Latency()
{
	//  there might be some jitter!
	return LATENCY + (rand() & 0x7fff) / (32768.0 / (JITTER + 1e-6));
}

void AnExtrapolator::SendPacket(Pos const &pos, double time)
{
	if ((rand() & 0x7fff) / 32768.0 < DROPRATE) {
		return;
	}
	Packet p;
	p.time = time;
	p.pos = pos;
	packetQueue[time + Latency()] = p;
}

void AnExtrapolator::DeliverPacket(Packet const &packet, double now)
{
	intPos.AddSample(packet.time, now, &packet.pos.x);
}

void AnExtrapolator::ReceivePackets(double now)
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

void AnExtrapolator::UpdateTime()
{
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
	myPos.x += double(cos(dir) * vel * dt);
	myPos.y -= double(sin(dir) * vel * dt);

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
		}
	}
	if (now >= lastSentTime + 1.0 / SENDRATE) {
		SendPacket(myPos, now);
		lastSentTime = now;
	}
}
