#pragma once

int AnInitializeUICore();
void AnUpdateUICore();
void AnInjectMouseMoveToUI(const cocos2d::Point& p);
void AnInjectMouseDownToUI(const cocos2d::Point& p);
void AnInjectMouseUpToUI(const cocos2d::Point& p);
