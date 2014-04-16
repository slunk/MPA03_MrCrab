/*  Defines claw movements. */
#include <Servo.h> 

const int LEFT_CLAW_PIN = 9;
const int RIGHT_CLAW_PIN = 10;
const int MIN_ANGLE = 0;
const int MAX_ANGLE = 110;

const int STATE_THRASH = 0;
const int STATE_PUSH_UP = 1;
const int STATE_THREATEN = 2;
const int STATE_WIND_UP = 3;
const int STATE_STAB_DOWN = 4;

volatile int state = STATE_WIND_UP;

Servo left_claw;
Servo right_claw;

// main ////////////////////////////////////////////////////////////////////////
void setup() { 
    left_claw.attach(LEFT_CLAW_PIN);
    right_claw.attach(RIGHT_CLAW_PIN);
    
    attachInterrupt(0, set_state_push_up, RISING);
    attachInterrupt(1, set_state_thrash, RISING);
    attachInterrupt(2, set_state_threaten, RISING);
    attachInterrupt(3, set_state_wind_up, RISING);
    attachInterrupt(4, set_state_stab_down, RISING);
} 
 
void loop() {
    //thrash();
    //wind_up();
    //stab_down();
    //thrash();
    //push_up();
    //threaten();
    switch (state) {
      case STATE_WIND_UP:
        wind_up();
        break;
      case STATE_PUSH_UP:
        push_up();
        break;
      case STATE_THRASH:
        thrash();
        break;
      case STATE_THREATEN:
        threaten();
        break;
      case STATE_STAB_DOWN:
        stab_down();
        break;
      default:
        wind_up();
        break;
    }
}

void set_state_wind_up() {
  state = STATE_WIND_UP;
}

void set_state_push_up() {
  state = STATE_PUSH_UP;
}

void set_state_thrash() {
  state = STATE_THRASH;
}

void set_state_threaten() {
  state = STATE_THREATEN;
}

void set_state_stab_down() {
  state = STATE_STAB_DOWN;
}

// actions /////////////////////////////////////////////////////////////////////
void push_up() {
    stab_down();
    delay(1000);
    wind_up();
    delay(1500);
}

void thrash() {
    move(MIN_ANGLE, MIN_ANGLE);
    delay(200);
    move(MAX_ANGLE, MAX_ANGLE);
    delay(200);
}

void threaten() {
    wind_up();
    delay(50);
    move(MAX_ANGLE - 10, MIN_ANGLE + 10);
    delay(50);
}

void shiver() {
    stab_down();
    delay(50);
    move(MIN_ANGLE + 10, MAX_ANGLE - 10);
    delay(50);
}

void taunt() {
    for (int i = 0; i < 10; i++) {
        shiver();
    }
    delay(1000);
}

void sneak() {
    for (int angle = MIN_ANGLE; angle <= MAX_ANGLE; angle++) {
        move(angle, angle);
        delay(100);
    }

    for (int angle = MAX_ANGLE; angle >= MIN_ANGLE; angle--) {
        move(angle, angle);
        delay(100);
    }
}


// movement primitives /////////////////////////////////////////////////////////

// stab the ground
void stab_down() {
    move(MIN_ANGLE, MAX_ANGLE);
}

// perpare for the next stab
void wind_up() {
    move(MAX_ANGLE, MIN_ANGLE);
}

void move(const int left_claw_angle, const int right_claw_angle) {
    left_claw.write(left_claw_angle);
    right_claw.write(right_claw_angle);
}
