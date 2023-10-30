/*
 * sh-astro-flat-panel-v1.ino
 * Copyright (C) 2023, Serhii Herniak - All Rights Reserved
 * Licensed under the MIT License. See the accompanying LICENSE file for terms.
 */

constexpr auto DEVICE_GUID = "6C69985D-0974-4599-8367-8628E4B3F0F0";

constexpr auto COMMAND_PING = "PING";
constexpr auto COMMAND_GETBRIGHTNESS = "GETBRIGHTNESS";
constexpr auto COMMAND_ON = "ON:";
constexpr auto COMMAND_OFF = "OFF";

constexpr auto RESULT_OK = "OK";
constexpr auto RESULT_INVALID_COMMAND = "NOK:INVALID_COMMAND";

#define MIN_BRIGHTNESS 0
#define MAX_BRIGHTNESS 255
#define LED_PIN 3

byte brightness = 0;

void setup() {
  Serial.begin(57600, SERIAL_8N1);
  Serial.setTimeout(100);
  while (!Serial);
  Serial.println("INITIALIZED#");

  TCCR2B = TCCR2B & B11111000 | B00000010; // for PWM frequency of 3921.16 Hz
  pinMode(LED_PIN, OUTPUT);
}

void loop() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    String response = processCommand(command);
    Serial.println(response);
  }
}

String processCommand(String command) {
  if (command == COMMAND_PING) {
    return String(RESULT_OK) + ":" + String(DEVICE_GUID);
  } else if (command == COMMAND_GETBRIGHTNESS) {
    return String(RESULT_OK) + ":" + String(brightness);
  } else if (command == COMMAND_OFF) {
    applyBrightness(0);
    return String(RESULT_OK);
  } else if (command.startsWith(COMMAND_ON)) {
    String arg = command.substring(strlen(COMMAND_ON));
    byte value = (byte) arg.toInt();
    applyBrightness(value);
    return String(RESULT_OK);
  } else {
    return String(RESULT_INVALID_COMMAND);
  }
}

void applyBrightness(byte newBrightness) {
  if (newBrightness < MIN_BRIGHTNESS) {
    brightness = MIN_BRIGHTNESS;
  } else if (newBrightness > MAX_BRIGHTNESS) {
    brightness = MAX_BRIGHTNESS;
  } else {
    brightness = newBrightness;
  }
  analogWrite(LED_PIN, brightness);
}
