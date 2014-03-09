#pragma once

extern double GServerTime;

int AnConnect();
void AnPollNetworkIoService();
void AnSendMove(int objectId, double x, double y);
