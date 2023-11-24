// Visual Micro is in vMicro>General>Tutorial Mode
// 
/*
    Name:       Mega_Serial_Com.ino
    Created:	12/12/2022 12:00:00 AM
    Author:     DESKTOP-RDPP2O1\Neuro42_Robot (Hamidreza Hoshyarmanesh)
*/


//the pin that controls the pulse
const int PulsePin_R = 52;       //Motor 2, Linear Translation, Right thumbstick
//the pin that controls the direction
const int DirectionPin_R = 53;

//the pin that controls the amplifier supply voltage (+5V) 
const int Motor_active_R = 43;

const int deadband = 6700;
const short short_MaxValue = 32767;

const byte numChars = 40;
const byte maxChars = 32;
char receivedChars[numChars];
float rightThumbStick = 0;
bool DIR_H = HIGH;
float rightThumbStick_mapped = 0;

char recvChar;
boolean newData = false;
char rc;

int rightThumbStick_old = 0; 


void setup() {
    // declare the ledPin as an OUTPUT:
    Serial.begin(57600);
    pinMode(DirectionPin_R, OUTPUT);
    pinMode(PulsePin_R, OUTPUT);
    pinMode(Motor_active_R, OUTPUT);
    digitalWrite(Motor_active_R, LOW);
}

void loop() {

    if (Serial.available() > 0)
    {
        recvWithStartEndMarkers(); 
             
    }
}
        

void recvWithStartEndMarkers() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    

    // while (Serial.available() > 0) {
    rc = Serial.read();
    if (recvInProgress == true) {
        if (rc != endMarker) {
            receivedChars[ndx] = rc;
            ndx++;
            if (ndx >= maxChars) {
                ndx = maxChars - 1;
            }
        }
        else {
            receivedChars[ndx] = '\0'; // terminate the string
            recvInProgress = false;
            ndx = 0;
            newData = true;
            parseData();
        }
    }

    else if (rc == startMarker) {
        recvInProgress = true;
    }

    Serial.flush();
    // }
}

void parseData() {

    // split the data into its parts
    if (newData == true) {
        char* strtokIndx; // this is used by strtok() as an index

        strtokIndx = strtok((char*)receivedChars, "<,>");
        rightThumbStick = atoi(strtokIndx);
        

        if (rightThumbStick > 0) {

            if (rightThumbStick >= deadband && (rightThumbStick * rightThumbStick_old > 0)) {
                digitalWrite(DirectionPin_R, !DIR_H);
                rightThumbStick_mapped = rightThumbStick / short_MaxValue * 100;
                Serial.println(rightThumbStick_mapped);
                digitalWrite(Motor_active_R, HIGH);
                pulsegeneration_R();
            }           
        }
        else if (rightThumbStick < 0 && (rightThumbStick * rightThumbStick_old > 0)){
            
            if (abs(rightThumbStick) >= deadband) {
                digitalWrite(DirectionPin_R, DIR_H);
                rightThumbStick_mapped = abs(rightThumbStick) / short_MaxValue * 100;
                Serial.println(rightThumbStick_mapped);
                digitalWrite(Motor_active_R, HIGH);
                pulsegeneration_R();
            }
        }
        else {
            digitalWrite(Motor_active_R, LOW);
        }
        
        
        newData = false;
        rightThumbStick_old = rightThumbStick;
    }
}

void pulsegeneration_R() {

    digitalWrite(PulsePin_R, LOW);
    delay(2000 / rightThumbStick_mapped);

    digitalWrite(PulsePin_R, HIGH);
    delay(2000 / rightThumbStick_mapped);
}






// // LED Test

// int LED = 13;

// void setup() {  // initialize digital pin 13 as an output.
//    pinMode(LED, OUTPUT);
// }

// // the loop function runs over and over again forever

// void loop() {
//    digitalWrite(LED, HIGH); // turn the LED on (HIGH is the voltage level)
//    delay(1000); // wait for a second
//    digitalWrite(LED, LOW); // turn the LED off by making the voltage LOW
//    delay(1000); // wait for a second
// }




// // Visual Micro
// // 
// /*
//     Name:       Leonardo_Serial_Com_Left.ino
//     Created:	12/12/2022 12:00:00 AM
//     Author:     DESKTOP-RDPP2O1\Neuro42_Robot (Hamidreza Hoshyarmanesh)
// */


// //the pin that controls the pulse
// const int PulsePin_R = 52;       //Motor 1, Rotation, Left thumbstick

// //the pin that controls the direction
// const int DirectionPin_R = 53;

// //the pin that controls the amplifier supply voltage (+5V) 
// const int Motor_active_R = 43;

// const int deadband = 6700;
// const short short_MaxValue = 32767.0;  //the max value stored in two bytes
// bool DIR_H = HIGH;
// const byte numChars = 32;
// char receivedChars[numChars];

// float rightThumbStick;

// float rightThumbStick_mapped;
// char recvChar;
// boolean newData = false;
// char rc;
// int rightThumbStick_old = 0; 


// void setup() {
//     // declare the ledPin as an OUTPUT:
//     Serial.begin(57600);
//     pinMode(DirectionPin_R, OUTPUT);
//     pinMode(PulsePin_R, OUTPUT);
//     pinMode(Motor_active_R, OUTPUT);
//     digitalWrite(Motor_active_R, LOW);
// }

// void loop() {

//     if (Serial.available() > 0)
//     {
//         recvWithStartEndMarkers();
//     }
    
// }


// void recvWithStartEndMarkers() {
//     static boolean recvInProgress = false;
//     static byte ndx = 0;
//     char startMarker = '<';
//     char endMarker = '>';


//     // while (Serial.available() > 0) {
//     rc = Serial.read();
//     if (recvInProgress == true) {
//         if (rc != endMarker) {
//             receivedChars[ndx] = rc;
//             ndx++;
//             if (ndx >= numChars) {
//                 ndx = numChars - 1;
//             }
//         }
//         else {
//             receivedChars[ndx] = '\0'; // terminate the string
//             recvInProgress = false;
//             ndx = 0;
//             newData = true;
//             parseData();
//         }
//     }

//     else if (rc == startMarker) {
//         recvInProgress = true;
//     }

//     Serial.flush();
    
//     // }
// }

// void parseData() {

//     // split the data into its parts
//     if (newData == true) {
//         char* strtokIndx; // this is used by strtok() as an index

//         strtokIndx = strtok((char*)receivedChars, "<,>");
//         rightThumbStick = atoi(strtokIndx);

//         if (rightThumbStick > 0 && (rightThumbStick * rightThumbStick_old > 0)) {
            
//             if (rightThumbStick >= deadband) {
//                 digitalWrite(DirectionPin_R, DIR_H);
//                 rightThumbStick_mapped = rightThumbStick / short_MaxValue * 100.0;
//                 digitalWrite(Motor_active_R, HIGH);
//                 pulsegeneration_R();
//             }
//         }
//         else if (rightThumbStick < 0 && (rightThumbStick * rightThumbStick_old > 0)) {
            
//             if (abs(rightThumbStick) >= deadband) {
//                 digitalWrite(DirectionPin_R, !DIR_H);
//                 rightThumbStick_mapped = abs(rightThumbStick) / short_MaxValue * 100.0;
//                 digitalWrite(Motor_active_R, HIGH);
//                 pulsegeneration_R();
//             }
//         }
//         else {
//             digitalWrite(Motor_active_R, LOW);
//         }

//         newData = false;
//         rightThumbStick_old = rightThumbStick;
//     }
// }

// void pulsegeneration_R() {

//     //unsigned long currentMillis = millis();
//     //interval = 4000./ rightThumbStick_mapped;
//     //if (currentMillis - previousMillis >= interval) {

//         //previousMillis = currentMillis;
//     digitalWrite(PulsePin_R, LOW);
//     delay(2000 / rightThumbStick_mapped);

//     digitalWrite(PulsePin_R, HIGH);
//     delay(2000 / rightThumbStick_mapped);

// }







// // ########################### Original #####################

// // Visual Micro is in vMicro>General>Tutorial Mode
// // 
// /*
//     Name:       Mega_Serial_Com.ino
//     Created:	12/12/2022 12:00:00 AM
//     Author:     DESKTOP-RDPP2O1\Neuro42_Robot (Hamidreza Hoshyarmanesh)
// */


// //the pin that controls the pulse
// const int PulsePin_R = 52;       //Motor 2, Linear Translation, Right thumbstick
// //the pin that controls the direction
// const int DirectionPin_R = 53;

// //the pin that controls the amplifier supply voltage (+5V) 
// const int Motor_active_R = 43;

// const int deadband = 6700;
// const short short_MaxValue = 32767;

// const byte numChars = 32;
// char receivedChars[numChars];
// float rightThumbStick = 0;
// bool DIR_H = HIGH;
// float rightThumbStick_mapped = 0;

// char recvChar;
// boolean newData = false;
// char rc;


// void setup() {
//     // declare the ledPin as an OUTPUT:
//     Serial.begin(9600);
//     pinMode(DirectionPin_R, OUTPUT);
//     pinMode(PulsePin_R, OUTPUT);
//     pinMode(Motor_active_R, OUTPUT);
//     digitalWrite(Motor_active_R, LOW);
// }

// void loop() {

//     if (Serial.available() > 0)
//     {
//         recvWithStartEndMarkers();      
//     }
// }
        

// void recvWithStartEndMarkers() {
//     static boolean recvInProgress = false;
//     static byte ndx = 0;
//     char startMarker = '<';
//     char endMarker = '>';
    

//     while (Serial.available() > 0) {
//         rc = Serial.read();
//         if (recvInProgress == true) {
//             if (rc != endMarker) {
//                 receivedChars[ndx] = rc;
//                 ndx++;
//                 if (ndx >= numChars) {
//                     ndx = numChars - 1;
//                 }
//             }
//             else {
//                 receivedChars[ndx] = '\0'; // terminate the string
//                 recvInProgress = false;
//                 ndx = 0;
//                 newData = true;
//                 parseData();
//             }
//         }

//         else if (rc == startMarker) {
//             recvInProgress = true;
//         }
//     }
// }

// void parseData() {

//     // split the data into its parts
//     if (newData == true) {
//         char* strtokIndx; // this is used by strtok() as an index

//         strtokIndx = strtok((char*)receivedChars, "<,>");
//         rightThumbStick = atoi(strtokIndx);

//         if (rightThumbStick > 0) {
//             digitalWrite(DirectionPin_R, DIR_H);
//             if (rightThumbStick >= deadband) {

//                 rightThumbStick_mapped = rightThumbStick / short_MaxValue * 100;
//                 Serial.println(rightThumbStick_mapped);
//                 digitalWrite(Motor_active_R, HIGH);
//                 pulsegeneration_R();
//             }           
//         }
//         else if (rightThumbStick < 0){
//             digitalWrite(DirectionPin_R, !DIR_H);
//             if (abs(rightThumbStick) >= deadband) {
//                 rightThumbStick_mapped = abs(rightThumbStick) / short_MaxValue * 100;
//                 Serial.println(rightThumbStick_mapped);
//                 digitalWrite(Motor_active_R, HIGH);
//                 pulsegeneration_R();
//             }
//         }
//         else {
//             digitalWrite(Motor_active_R, LOW);
//         }
        
        
//         newData = false;
//     }
// }

// void pulsegeneration_R() {

//     digitalWrite(PulsePin_R, LOW);
//     delay(2000 / rightThumbStick_mapped);

//     digitalWrite(PulsePin_R, HIGH);
//     delay(2000 / rightThumbStick_mapped);
// }


