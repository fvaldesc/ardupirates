/*
 www.ArduCopter.com - www.DIYDrones.com
 Copyright (c) 2010.  All rights reserved.
 An Open Source Arduino based multicopter.
 
 File     : ArducopterNG.pde
 Version  : v1.0, 11 October 2010
 Author(s): ArduCopter Team
 Ted Carancho (AeroQuad), Jose Julio, Jordi Muñoz,
 Jani Hirvinen, Ken McEwans, Roberto Navoni,          
 Sandro Benigno, Chris Anderson

 Author(s) : ArduPirates deveopment team                                  
          Philipp Maloney, Norbert, Hein, Igor, Emile, Kidogo 
          
 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with this program. If not, see <http://www.gnu.org/licenses/>.
 
/* ********************************************************************** */
/* Hardware : ArduPilot Mega + Sensor Shield (Production versions)        */
/* Mounting position : RC connectors pointing backwards                   */
/* This code use this libraries :                                         */
/*   APM_RC : Radio library (with InstantPWM)                             */
/*   AP_ADC : External ADC library                                        */
/*   DataFlash : DataFlash log library                                    */
/*   APM_BMP085 : BMP085 barometer library                                */
/*   AP_Compass : HMC5843 compass library [optional]                      */
/*   GPS_MTK or GPS_UBLOX or GPS_NMEA : GPS library    [optional]         */
/* ********************************************************************** */

/*
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

WARNING: This file now only contains program logic. You need not edit
         this file to change any settings for your multicopter.
         Any configuration is done in config.h.
         
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
*/

/* ************************************************************ */
/* **************** MAIN PROGRAM - DEFINES ******************** */
/* ************************************************************ */
#include "Config.h" // [kidogo] Moved all user configurable settings 
                      // to config.h, formerly ArduUser.h
                      
/* ************************************************************ */
/* **************** MAIN PROGRAM - INCLUDES ******************* */
/* ************************************************************ */

#include <avr/io.h>
#include <avr/eeprom.h>
#include <avr/pgmspace.h>
#include <FastSerial.h>
#include <AP_Common.h>
#include <math.h>
#include <APM_RC.h> 		// ArduPilot Mega RC Library
#include <AP_ADC.h>		// ArduPilot Mega Analog to Digital Converter Library 
#include <APM_BMP085.h> 	// ArduPilot Mega BMP085 Library 
#include <DataFlash.h>		// ArduPilot Mega Flash Memory Library
#include <AP_Compass.h>	        // ArduPilot Mega Magnetometer Library
#include <Wire.h>               // I2C Communication library
#include <EEPROM.h>             // EEPROM 
#include <AP_RangeFinder.h>     // RangeFinders (Sonars, IR Sensors)
#include <AP_GPS.h>
#include "Arducopter.h"

#if AIRFRAME == HELI
#include "Heli.h"
#endif

/* Software version */
#define VER 2.01    // Current software version (only numeric values)

// Sensors - declare one global instance
AP_ADC_ADS7844		adc;
APM_BMP085_Class	APM_BMP085;
AP_Compass_HMC5843	AP_Compass;
#ifdef IsSONAR
AP_RangeFinder_MaxsonarXL  AP_RangeFinder_down;  // Default sonar for altitude hold
//AP_RangeFinder_MaxsonarLV  AP_RangeFinder_down;  // Alternative sonar is AP_RangeFinder_MaxsonarLV
#endif
#ifdef IsRANGEFINDER
AP_RangeFinder_MaxsonarLV  AP_RangeFinder_frontRight;
AP_RangeFinder_MaxsonarLV  AP_RangeFinder_backRight;
AP_RangeFinder_MaxsonarLV  AP_RangeFinder_backLeft;
AP_RangeFinder_MaxsonarLV  AP_RangeFinder_frontLeft;
#endif


/* ************************************************************ */
/* ************* MAIN PROGRAM - DECLARATIONS ****************** */
/* ************************************************************ */

byte flightMode;

unsigned long currentTime;  // current time in milliseconds
unsigned long currentTimeMicros = 0, previousTimeMicros = 0;  // current and previous loop time in microseconds
unsigned long mainLoop = 0;
unsigned long mediumLoop = 0;
unsigned long slowLoop = 0;

/* ************************************************************ */
/* **************** MAIN PROGRAM - SETUP ********************** */
/* ************************************************************ */
void setup() {

  APM_Init();                // APM Hardware initialization (in System.pde)

  mainLoop = millis();       // Initialize timers
  mediumLoop = mainLoop;
  GPS_timer = mainLoop;
  motorArmed = 0;
  
  GEOG_CORRECTION_FACTOR = 0;   // Geographic correction factor will be automatically calculated

  Read_adc_raw();            // Initialize ADC readings...
  
#ifdef SerXbee
  Serial.begin(SerBau);
  Serial.print("ArduCopter v");
  Serial.println(VER);
  Serial.println("Serial data on Telemetry port");
  Serial.println("No commands or output on this serial, check your Arducopter.pde if needed to change.");
  Serial.println();
  Serial.println("General info:");
  if(!SW_DIP1) Serial.println("Flight mode: + ");
  if(SW_DIP1) Serial.println("Flight mode: x ");
#endif 


  delay(10);
  digitalWrite(LED_Green,HIGH);     // Ready to go...  
}


/* ************************************************************ */
/* ************** MAIN PROGRAM - MAIN LOOP ******************** */
/* ************************************************************ */

// Sensor reading loop is inside AP_ADC and runs at 400Hz (based on Timer2 interrupt)

// * fast rate loop => Main loop => 200Hz
// read sensors
// IMU : update attitude
// motor control
// Asyncronous task : read transmitter
// * medium rate loop (60Hz)
// Asyncronous task : read GPS
// * slow rate loop (10Hz)
// magnetometer
// barometer (20Hz)
// sonar (20Hz)
// obstacle avoidance (20Hz)
// external command/telemetry
// Battery monitor



/* ***************************************************** */
// Main loop 
void loop()
{
  
  currentTimeMicros = micros();
  currentTime = currentTimeMicros / 1000;

  // Main loop at 200Hz (IMU + control)
  if ((currentTime-mainLoop) > 5)    // about 200Hz (every 5ms)
  {
    G_Dt = (currentTimeMicros-previousTimeMicros) * 0.000001;   // Microseconds!!!
    mainLoop = currentTime;
    previousTimeMicros = currentTimeMicros;

    //IMU DCM Algorithm
    Read_adc_raw();       // Read sensors raw data
    Matrix_update(); 
    Normalize();          
    Drift_correction();
    Euler_angles();

    // Read radio values (if new data is available)
    if (APM_RC.GetState() == 1) {  // New radio frame?
#if ((AIRFRAME == QUAD) || (AIRFRAME == HEXA) || (AIRFRAME == OCTA))   
      read_radio();
#endif
#if AIRFRAME == HELI
      heli_read_radio();
#endif
#if (defined(SerXbee) && defined(Use_PID_Tuning))  
      PID_Tuning();  // See Functions.
#endif
    }

    // Attitude control
    if(flightMode == FM_STABLE_MODE) {    // STABLE Mode
      gled_speed = 1200;
      if (AP_mode == AP_NORMAL_STABLE_MODE) {   // Normal mode
#if ((AIRFRAME == QUAD) || (AIRFRAME == HEXA) || (AIRFRAME == OCTA))
        Attitude_control_v3(command_rx_roll,command_rx_pitch,command_rx_yaw);
#endif        
#if AIRFRAME == HELI
        heli_attitude_control(command_rx_roll,command_rx_pitch,command_rx_collective,command_rx_yaw);
#endif
      }else{                        // Automatic mode : GPS position hold mode
#if ((AIRFRAME == QUAD) || (AIRFRAME == HEXA) || (AIRFRAME == OCTA))
        Attitude_control_v3(command_rx_roll+command_gps_roll+command_RF_roll,command_rx_pitch+command_gps_pitch+command_RF_pitch,command_rx_yaw);
#endif        
#if AIRFRAME == HELI
        heli_attitude_control(command_rx_roll+command_gps_roll,command_rx_pitch+command_gps_pitch,command_rx_collective,command_rx_yaw);
#endif
      }
    }
    else {                 // ACRO Mode
      gled_speed = 400;
      Rate_control_v2();
      // Reset yaw, so if we change to stable mode we continue with the actual yaw direction
      command_rx_yaw = ToDeg(yaw);
    }

    // Send output commands to motor ESCs...
#if ((AIRFRAME == QUAD) || (AIRFRAME == HEXA) || (AIRFRAME == OCTA))
    motor_output();
#endif  

#ifdef IsCAM
  // Do we have cameras stabilization connected and in use?
  if(!SW_DIP2){ 
    camera_output();
#ifdef UseCamShutter
    CamTrigger();
#endif
  }
#endif

    // Autopilot mode functions - GPS Hold, Altitude Hold + object avoidance
    if (AP_mode == AP_GPS_HOLD || AP_mode == AP_ALT_GPS_HOLD)
    {
      // Do GPS Position hold (latitude & longitude)
      if (target_position) 
      {
        #ifdef IsGPS
        if (gps.new_data)     // New GPS info?
        {
          if (gps.fix)
          {
            read_GPS_data();    // In Navigation.pde
            //Position_control(target_latitude,target_longitude);     // Call GPS position hold routine
            Position_control_v2(target_latitude,target_longitude);     // V2 of GPS Position holdCall GPS position hold routine
          }
          else
          {
            command_gps_roll=0;
            command_gps_pitch=0;
          }
        }
        #endif
      } else {  // First time we enter in GPS position hold we capture the target position as the actual position
        #ifdef IsGPS
        if (gps.fix){   // We need a GPS Fix to capture the actual position...
          target_latitude = gps.latitude;
          target_longitude = gps.longitude;
          target_position=1;
        }
        #endif
        command_gps_roll=0;
        command_gps_pitch=0;
        Reset_I_terms_navigation();  // Reset I terms (in Navigation.pde)
      }
    }
    if (AP_mode == AP_ALTITUDE_HOLD || AP_mode == AP_ALT_GPS_HOLD)
    {
     // Switch on altitude control if we have a barometer or Sonar
      #if (defined(UseBMP) || defined(IsSONAR))
      if( altitude_control_method == ALTITUDE_CONTROL_NONE ) 
      {
          // by default turn on altitude hold using barometer
          #ifdef UseBMP
          if( press_baro_altitude != 0 ) 
          {
            altitude_control_method = ALTITUDE_CONTROL_BARO;  
            target_baro_altitude = press_baro_altitude;  
            baro_altitude_I = 0;  // don't carry over any I values from previous times user may have switched on altitude control
          }
          #endif
          
          // use sonar if it's available
          #ifdef IsSONAR
          if( sonar_status == SONAR_STATUS_OK && press_sonar_altitude != 0 ) 
          {
            altitude_control_method = ALTITUDE_CONTROL_SONAR;
            target_sonar_altitude = constrain(press_sonar_altitude,AP_RangeFinder_down.min_distance*3,sonar_threshold);
          }
          sonar_altitude_I = 0;  // don't carry over any I values from previous times user may have switched on altitude control          
          #endif
          
          // capture current throttle to use as base for altitude control
          initial_throttle = ch_throttle;
          ch_throttle_altitude_hold = ch_throttle;      
      }
 
      // Sonar Altitude Control
      #ifdef IsSONAR 
      if( sonar_new_data ) // if new sonar data has arrived
      {
        // Allow switching between sonar and barometer
        #ifdef UseBMP
        
        // if SONAR become invalid switch to barometer
        if( altitude_control_method == ALTITUDE_CONTROL_SONAR && sonar_valid_count <= -3  )
        {
          // next target barometer altitude to current barometer altitude + user's desired change over last sonar altitude (i.e. keeps up the momentum)
          altitude_control_method = ALTITUDE_CONTROL_BARO;
          target_baro_altitude = press_baro_altitude;// + constrain((target_sonar_altitude - press_sonar_altitude),-50,50);
        }
   
        // if SONAR becomes valid switch to sonar control
        if( altitude_control_method == ALTITUDE_CONTROL_BARO && sonar_valid_count >= 3  )
        {
          altitude_control_method = ALTITUDE_CONTROL_SONAR;
          if( target_sonar_altitude == 0 ) {  // if target sonar altitude hasn't been intialised before..
            target_sonar_altitude = press_sonar_altitude;// + constrain((target_baro_altitude - press_baro_altitude),-50,50);  // maybe this should just use the user's last valid target sonar altitude         
          }
          // ensure target altitude is reasonable
          target_sonar_altitude = constrain(target_sonar_altitude,AP_RangeFinder_down.min_distance*3,sonar_threshold);
        }      
        #endif  // defined(UseBMP)
       
        // main Sonar control
        if( altitude_control_method == ALTITUDE_CONTROL_SONAR )
        {
          if( sonar_status == SONAR_STATUS_OK ) {
            ch_throttle_altitude_hold = Altitude_control_Sonar(press_sonar_altitude,target_sonar_altitude);  // calculate throttle to maintain altitude
          } else {
            // if sonar_altitude becomes invalid we return control to user temporarily
            ch_throttle_altitude_hold = ch_throttle;
          }            
  
          // modify the target altitude if user moves stick more than 100 up or down
          if( abs(ch_throttle-initial_throttle)>100 ) 
          {
            target_sonar_altitude += (ch_throttle-initial_throttle)/25;
            if( target_sonar_altitude < AP_RangeFinder_down.min_distance*3 )
                target_sonar_altitude = AP_RangeFinder_down.min_distance*3;
            #if !defined(UseBMP)   // limit the upper altitude if no barometer used
            if( target_sonar_altitude > sonar_threshold)
                target_sonar_altitude = sonar_threshold;
            #endif     
          }
        }
        sonar_new_data = 0;  // record that we've consumed the sonar data
      }  // new sonar data
      #endif // #ifdef IsSONAR
      
      // Barometer Altitude control
      #ifdef UseBMP
      if( baro_new_data && altitude_control_method == ALTITUDE_CONTROL_BARO )   // New altitude data?
      {
        ch_throttle_altitude_hold = Altitude_control_baro(press_baro_altitude,target_baro_altitude);   // calculate throttle to maintain altitude
        baro_new_data=0;  // record that we have consumed the new data
        
        // modify the target altitude if user moves stick more than 100 up or down
        if (abs(ch_throttle-initial_throttle)>100) 
        {
          target_baro_altitude += (ch_throttle-initial_throttle)/25;  // Change in stick position => altitude ascend/descend rate control
        }
      }
      #endif
      #endif // defined(UseBMP) || defined(IsSONAR)
      
      // Object avoidance
      #ifdef IsRANGEFINDER
      if( RF_new_data ) 
      {
        Obstacle_avoidance(RF_SAFETY_ZONE);  // main obstacle avoidance function
        RF_new_data = 0;  // record that we have consumed the rangefinder data
      }
      #endif
    }
    if (AP_mode == AP_NORMAL_STABLE_MODE)
    {
//      digitalWrite(LED_Yellow,LOW);
      target_position=0;
      if( altitude_control_method != ALTITUDE_CONTROL_NONE )
      {
        altitude_control_method = ALTITUDE_CONTROL_NONE;  // turn off altitude control
      }
    } 
  }

  // Medium loop (about 60Hz) 
  if ((currentTime-mediumLoop)>=17){
    mediumLoop = currentTime;
#ifdef IsGPS
    gps.update();   // Read GPS data
#endif
    
#if AIRFRAME == HELI    
    // Send output commands to heli swashplate...
    heli_moveSwashPlate();    // we update the heli swashplate at about 60hz
#endif

    // Each of the six cases executes at 10Hz
    switch (medium_loopCounter){
    case 0:   // Magnetometer reading (10Hz)
      medium_loopCounter++;
      slowLoop++;
#ifdef IsMAG
      if (MAGNETOMETER == 1) {
        AP_Compass.read();     // Read magnetometer
        AP_Compass.calculate(roll,pitch);  // Calculate heading
      }
#endif
      break;
    case 1:  // Barometer + RangeFinder reading (2x10Hz = 20Hz)
      medium_loopCounter++;
#ifdef UseBMP
      if (APM_BMP085.Read()){
        read_baro();
        baro_new_data = 1;
      }
#endif
#ifdef IsSONAR
      read_Sonar(); 
      sonar_new_data = 1;  // process sonar values at 20Hz     
#endif
#ifdef IsRANGEFINDER
      read_RF_Sensors();
      RF_new_data = 1;      
#endif
      break;
    case 2:  // Send serial telemetry (10Hz)
      medium_loopCounter++;
#ifdef CONFIGURATOR
      sendSerialTelemetry();
#endif
      break;
    case 3:  // Read serial telemetry (10Hz)
      medium_loopCounter++;
#ifdef CONFIGURATOR
      readSerialCommand();
#endif
      break;
    case 4:  // second Barometer + RangeFinder reading (2x10Hz = 20Hz)
      medium_loopCounter++;
#ifdef UseBMP
      if (APM_BMP085.Read()){
        read_baro();
        baro_new_data = 1;
      }
#endif
#ifdef IsSONAR
      read_Sonar(); 
      sonar_new_data = 1;  // process sonar values at 20Hz     
#endif
#ifdef IsRANGEFINDER
      read_RF_Sensors();
      RF_new_data = 1;
#endif
      break;
    case 5:  //  Battery monitor (10Hz)
      medium_loopCounter=0;
#if BATTERY_EVENT == 1
      read_battery();         // Battery monitor
#endif
      break;	
    }
  }

  // AM and Mode status LED lights
  if(millis() - gled_timer > gled_speed) {
    gled_timer = millis();
    if(gled_status == HIGH) { 
      digitalWrite(LED_Green, LOW);
      if (flightMode == FM_ACRO_MODE)
        digitalWrite(LED_Yellow, LOW);
#ifdef IsGPS
      if ((AP_mode == AP_GPS_HOLD || AP_mode == AP_ALT_GPS_HOLD) && gps.fix < 1)      // Position Hold (GPS position control)
        digitalWrite(LED_Red,LOW);      // Red LED OFF : GPS not FIX
#endif

#ifdef IsAM      
      digitalWrite(RE_LED, LOW);
#endif
      gled_status = LOW;
//      SerPrln("L");
    } 
    else {
      digitalWrite(LED_Green, HIGH);
      if (flightMode == FM_ACRO_MODE)
        digitalWrite(LED_Yellow, HIGH);
#ifdef IsGPS
      if ((AP_mode == AP_GPS_HOLD || AP_mode == AP_ALT_GPS_HOLD) && gps.fix < 1)      // Position Hold (GPS position control)
        digitalWrite(LED_Red,HIGH);      // Red LED ON : GPS not FIX
#endif

#ifdef IsAM
      if(motorArmed) digitalWrite(RE_LED, HIGH);
#endif
      gled_status = HIGH;
    } 
  }

}


