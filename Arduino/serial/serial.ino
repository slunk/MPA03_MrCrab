/*
  Serial Event example

 When new serial data arrives, this sketch adds it to a String.
 When a newline is received, the loop prints the string and
 clears it.

 A good test for this is to try it with a GPS receiver
 that sends out NMEA 0183 sentences.

 Created 9 May 2011
 by Tom Igoe

 This example code is in the public domain.

 http://www.arduino.cc/en/Tutorial/SerialEvent

 */

String inputString = "";         // a string to hold incoming data
boolean stringComplete = false;  // whether the string is complete

const int PIN_THREATEN = 2;
const int PIN_WIND_UP = 3;
const int PIN_THRASH = 4;
const int PIN_PUSH_UP = 5;
const int PIN_STAB_DOWN = 6;

int prev_state = PIN_WIND_UP;

void setup() {
  // initialize serial:
  pinMode(PIN_THREATEN, OUTPUT);
  pinMode(PIN_WIND_UP, OUTPUT);
  pinMode(PIN_THRASH, OUTPUT);
  pinMode(PIN_PUSH_UP, OUTPUT);
  pinMode(PIN_STAB_DOWN, OUTPUT);
  Serial.begin(9600);
  // reserve 200 bytes for the inputString:
  inputString.reserve(200);
}

int strToInt(String str) {
  return str.charAt(0) - '0';
}

void turnOnThenOff(int pin) {
  if (pin != prev_state) {
    digitalWrite(pin, HIGH);
    delay(50);
    digitalWrite(pin, LOW);
    delay(50);
    prev_state = pin;
  }
}

void handleInput(String numPeople, String distanceToHome) {
  if (numPeople == "0") {
    turnOnThenOff(PIN_WIND_UP);
  } else if (numPeople == "1") {
    if (distanceToHome == "close") {
      turnOnThenOff(PIN_THREATEN);
    } else {
      turnOnThenOff(PIN_STAB_DOWN);
    }
  } else {
    if (distanceToHome == "close") {
      turnOnThenOff(PIN_THRASH);
    } else {
      turnOnThenOff(PIN_STAB_DOWN);
    }
  }
}

void loop() {
  if (Serial.available() > 0) {
    String numPeople = Serial.readStringUntil(',');
    String distanceToHome = Serial.readStringUntil('\n');
    handleInput(numPeople, distanceToHome);
  }
}
