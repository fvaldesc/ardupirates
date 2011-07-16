// -*- tab-width: 4; Mode: C++; c-basic-offset: 4; indent-tabs-mode: nil -*-

#ifndef PARAMETERS_H
#define PARAMETERS_H

#include <AP_Common.h>

// Global parameter class.
//
class Parameters {
public:
    // The version of the layout as described by the parameter enum.
    //
    // When changing the parameter enum in an incompatible fashion, this
    // value should be incremented by one.
    //
    // The increment will prevent old parameters from being used incorrectly
    // by newer code.
    //
    static const uint16_t k_format_version = 101;

	// The parameter software_type is set up solely for ground station use
	// and identifies the software type (eg ArduPilotMega versus ArduCopterMega)
	// GCS will interpret values 0-9 as ArduPilotMega.  Developers may use
	// values within that range to identify different branches.
	//
    static const uint16_t k_software_type = 10;		// 0 for APM trunk

    //
    // Parameter identities.
    //
    // The enumeration defined here is used to ensure that every parameter
    // or parameter group has a unique ID number.  This number is used by
    // AP_Var to store and locate parameters in EEPROM.
    //
    // Note that entries without a number are assigned the next number after
    // the entry preceding them.  When adding new entries, ensure that they
    // don't overlap.
    //
    // Try to group related variables together, and assign them a set
    // range in the enumeration.  Place these groups in numerical order
    // at the end of the enumeration.
    //
    // WARNING: Care should be taken when editing this enumeration as the
    //          AP_Var load/save code depends on the values here to identify
    //          variables saved in EEPROM.
    //
    //
    enum {
        // Layout version number, always key zero.
        //
        k_param_format_version = 0,
		k_param_software_type,


        // Misc
        //
        k_param_log_bitmask,

		// 110: Telemetry control
		//
		k_param_streamrates_port0 = 110,
		k_param_streamrates_port3,
		k_param_sysid_this_mav,
		k_param_sysid_my_gcs,

        //
        // 140: Sensor parameters
        //
        k_param_IMU_calibration = 140,
		k_param_battery_monitoring,
		k_param_pack_capacity,
		k_param_compass_enabled,
		k_param_compass,
		k_param_sonar,
		k_param_frame_orientation,

        //
        // 160: Navigation parameters
        //
        k_param_crosstrack_gain = 160,
        k_param_crosstrack_entry_angle,
        k_param_pitch_max,
        k_param_RTL_altitude,

        //
        // 180: Radio settings
        //
        k_param_rc_1 = 180,
        k_param_rc_2,
        k_param_rc_3,
        k_param_rc_4,
        k_param_rc_5,
        k_param_rc_6,
        k_param_rc_7,
        k_param_rc_8,
        k_param_rc_9,
        k_param_rc_10,
        k_param_throttle_min,
        k_param_throttle_max,
        k_param_throttle_fs_enabled,
        k_param_throttle_fs_action,
        k_param_throttle_fs_value,
        k_param_throttle_cruise,
        k_param_flight_modes,
        k_param_esc_calibrate,

 	   #if FRAME_CONFIG ==	HELI_FRAME
		//
		// 200: Heli
		//
		k_param_heli_servo_1 = 200,
		k_param_heli_servo_2,
		k_param_heli_servo_3,
		k_param_heli_servo_4,
		k_param_heli_servo1_pos ,
		k_param_heli_servo2_pos,
		k_param_heli_servo3_pos,
		k_param_heli_roll_max,
		k_param_heli_pitch_max,
		k_param_heli_collective_min,
		k_param_heli_collective_max,
		k_param_heli_collective_mid,
		k_param_heli_ext_gyro_enabled,
		k_param_heli_ext_gyro_gain,  // 213
		#endif

        //
        // 220: Waypoint data
        //
        k_param_waypoint_mode = 220,
        k_param_waypoint_total,
        k_param_waypoint_index,
        k_param_command_must_index,
        k_param_waypoint_radius,
        k_param_loiter_radius,

        //
        // 240: PID Controllers
        //
        // Heading-to-roll PID:
        // heading error from commnd to roll command deviation from trim
        // (bank to turn strategy)
        //
		k_param_pid_acro_rate_roll = 240,
		k_param_pid_acro_rate_pitch,
		k_param_pid_acro_rate_yaw,
		k_param_pid_stabilize_roll,
		k_param_pid_stabilize_pitch,
		k_param_pid_yaw,
		k_param_pid_nav_lat,
		k_param_pid_nav_lon,
		k_param_pid_nav_wp,
		k_param_pid_baro_throttle,
		k_param_pid_sonar_throttle,


        // 255: reserved
    };

    AP_Int16    format_version;
	AP_Int8		software_type;

	// Telemetry control
	//
	AP_Int16		sysid_this_mav;
	AP_Int16		sysid_my_gcs;


    // Crosstrack navigation
    //
    AP_Float    crosstrack_gain;
    AP_Int16    crosstrack_entry_angle;

    // Waypoints
    //
    AP_Int8     waypoint_mode;
    AP_Int8     waypoint_total;
    AP_Int8     waypoint_index;
    AP_Int8		command_must_index;
    AP_Int8     waypoint_radius;
    AP_Int8     loiter_radius;

    // Throttle
    //
    AP_Int16    throttle_min;
    AP_Int16    throttle_max;
    AP_Int8     throttle_fs_enabled;
    AP_Int8     throttle_fs_action;
    AP_Int16    throttle_fs_value;
    AP_Int16    throttle_cruise;

    // Flight modes
    //
    AP_VarA<uint8_t,6> flight_modes;

    // Radio settings
    //
    //AP_Var_group pwm_roll;
    //AP_Var_group pwm_pitch;
    //AP_Var_group pwm_throttle;
    //AP_Var_group pwm_yaw;

    AP_Int16     pitch_max;

    // Misc
    //
    AP_Int16    log_bitmask;
    AP_Int16    RTL_altitude;

    AP_Int8		sonar_enabled;
    AP_Int8		battery_monitoring;	// 0=disabled, 1=3 cell lipo, 2=4 cell lipo, 3=total voltage only, 4=total voltage and current
	AP_Int16	pack_capacity;		// Battery pack capacity less reserve
    AP_Int8		compass_enabled;
	AP_Int8		esc_calibrate;
	AP_Int8		frame_orientation;

    #if FRAME_CONFIG ==	HELI_FRAME
	// Heli
	RC_Channel	heli_servo_1, heli_servo_2, heli_servo_3, heli_servo_4;  // servos for swash plate and tail
	AP_Int16	heli_servo1_pos, heli_servo2_pos, heli_servo3_pos;       // servo positions (3 because we don't need pos for tail servo)
	AP_Int16	heli_roll_max, heli_pitch_max;   // maximum allowed roll and pitch of swashplate
	AP_Int16	heli_coll_min, heli_coll_max, heli_coll_mid;    // min and max collective.  mid = main blades at zero pitch
	AP_Int8		heli_ext_gyro_enabled;   // 0 = no external tail gyro, 1 = external tail gyro
	AP_Int16	heli_ext_gyro_gain;      // radio output 1000~2000 (value output on CH_7)
	#endif

    // RC channels
	RC_Channel	rc_1;
	RC_Channel	rc_2;
	RC_Channel	rc_3;
	RC_Channel	rc_4;
	RC_Channel	rc_5;
	RC_Channel	rc_6;
	RC_Channel	rc_7;
	RC_Channel	rc_8;
	RC_Channel	rc_camera_pitch;
	RC_Channel	rc_camera_roll;

    // PID controllers
	PID			pid_acro_rate_roll;
	PID			pid_acro_rate_pitch;
	PID			pid_acro_rate_yaw;
	PID			pid_stabilize_roll;
	PID			pid_stabilize_pitch;
	PID			pid_yaw;
	PID			pid_nav_lat;
	PID			pid_nav_lon;
	PID			pid_nav_wp;
	PID			pid_baro_throttle;
	PID			pid_sonar_throttle;

    uint8_t     junk;

    // Note: keep initializers here in the same order as they are declared above.
    Parameters() :
        // variable             default                     key										name
        //-------------------------------------------------------------------------------------------------------------------
        format_version          (k_format_version,          k_param_format_version,         		PSTR("SYSID_SW_MREV")),
        software_type			(k_software_type,			k_param_software_type,         			PSTR("SYSID_SW_TYPE")),

        sysid_this_mav			(MAV_SYSTEM_ID,				k_param_sysid_this_mav,					PSTR("SYSID_THISMAV")),
        sysid_my_gcs			(255,		                k_param_sysid_my_gcs,					PSTR("SYSID_MYGCS")),

        crosstrack_gain         (XTRACK_GAIN * 100,			k_param_crosstrack_gain,        		PSTR("XTRK_GAIN")),
        crosstrack_entry_angle  (XTRACK_ENTRY_ANGLE * 100,	k_param_crosstrack_entry_angle, 		PSTR("XTRACK_ANGLE")),

        sonar_enabled  			(DISABLED,					k_param_sonar,							PSTR("SONAR_ENABLE")),
        battery_monitoring 		(DISABLED,					k_param_battery_monitoring,				PSTR("BATT_MONITOR")),
        pack_capacity	 		(HIGH_DISCHARGE,			k_param_pack_capacity,					PSTR("BATT_CAPACITY")),
        compass_enabled			(MAGNETOMETER,				k_param_compass_enabled,				PSTR("MAG_ENABLE")),

        waypoint_mode           (0,                         k_param_waypoint_mode,          		PSTR("WP_MODE")),
        waypoint_total          (0,                         k_param_waypoint_total,         		PSTR("WP_TOTAL")),
        waypoint_index          (0,                         k_param_waypoint_index,         		PSTR("WP_INDEX")),
        command_must_index      (0,                         k_param_command_must_index,     		PSTR("WP_MUST_INDEX")),
        waypoint_radius         (WP_RADIUS_DEFAULT,         k_param_waypoint_radius,        		PSTR("WP_RADIUS")),
        loiter_radius           (LOITER_RADIUS_DEFAULT,     k_param_loiter_radius,          		PSTR("LOITER_RADIUS")),

        throttle_min            (0,             			k_param_throttle_min,					PSTR("THR_MIN")),
        throttle_max            (1000, 			            k_param_throttle_max,					PSTR("THR_MAX")),
        throttle_fs_enabled   	(THROTTLE_FAILSAFE,         k_param_throttle_fs_enabled,			PSTR("THR_FAILSAFE")),
        throttle_fs_action		(THROTTLE_FAILSAFE_ACTION,  k_param_throttle_fs_action, 			PSTR("THR_FS_ACTION")),
        throttle_fs_value 		(THROTTLE_FS_VALUE,         k_param_throttle_fs_value, 				PSTR("THR_FS_VALUE")),
        throttle_cruise         (100,						k_param_throttle_cruise,    			PSTR("TRIM_THROTTLE")),

        flight_modes            (k_param_flight_modes,                                     			PSTR("FLTMODE")),

        pitch_max         		(PITCH_MAX * 100,			k_param_pitch_max,		       			PSTR("PITCH_MAX")),

        log_bitmask             (MASK_LOG_SET_DEFAULTS,		k_param_log_bitmask,            		PSTR("LOG_BITMASK")),
        RTL_altitude            (ALT_HOLD_HOME * 100,		k_param_RTL_altitude,          			PSTR("ALT_HOLD_RTL")),
        esc_calibrate 			(0, 						k_param_esc_calibrate, 					PSTR("ESC")),

        frame_orientation 		(FRAME_ORIENTATION, 		k_param_frame_orientation, 				PSTR("FRAME")),

	    #if FRAME_CONFIG ==	HELI_FRAME
		heli_servo_1			(k_param_heli_servo_1,		PSTR("HS1_")),
		heli_servo_2			(k_param_heli_servo_2,		PSTR("HS2_")),
		heli_servo_3			(k_param_heli_servo_3,		PSTR("HS3_")),
		heli_servo_4			(k_param_heli_servo_4,		PSTR("HS4_")),
		heli_servo1_pos			(-60,						k_param_heli_servo1_pos,				PSTR("SV1_POS_")),
		heli_servo2_pos			(60,						k_param_heli_servo2_pos,				PSTR("SV2_POS_")),
		heli_servo3_pos			(180,						k_param_heli_servo3_pos,				PSTR("SV3_POS_")),
		heli_roll_max			(4500,						k_param_heli_roll_max,					PSTR("ROL_MAX_")),
		heli_pitch_max			(4500,						k_param_heli_pitch_max,					PSTR("PIT_MAX_")),
		heli_coll_min			(1000,						k_param_heli_collective_min,			PSTR("COL_MIN_")),
		heli_coll_max			(2000,						k_param_heli_collective_max,			PSTR("COL_MAX_")),
		heli_coll_mid			(1500,						k_param_heli_collective_mid,			PSTR("COL_MID_")),
		heli_ext_gyro_enabled	(0,							k_param_heli_ext_gyro_enabled,			PSTR("GYR_ENABLE_")),
		heli_ext_gyro_gain		(1000,						k_param_heli_ext_gyro_gain,				PSTR("GYR_GAIN_")),
		#endif

        // RC channel           group key                   name
        //----------------------------------------------------------------------
        rc_1					(k_param_rc_1,		PSTR("RC1_")),
        rc_2					(k_param_rc_2,		PSTR("RC2_")),
        rc_3					(k_param_rc_3,		PSTR("RC3_")),
        rc_4					(k_param_rc_4,		PSTR("RC4_")),
        rc_5					(k_param_rc_5,		PSTR("RC5_")),
        rc_6					(k_param_rc_6,		PSTR("RC6_")),
        rc_7					(k_param_rc_7,		PSTR("RC7_")),
        rc_8					(k_param_rc_8,		PSTR("RC8_")),
        rc_camera_pitch			(k_param_rc_9,		NULL),
        rc_camera_roll			(k_param_rc_10,		NULL),

        // PID controller   group key						name				initial P			initial I			initial D			initial imax
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
		pid_acro_rate_roll	(k_param_pid_acro_rate_roll,	PSTR("ACR_RLL_"),	ACRO_RATE_ROLL_P,   ACRO_RATE_ROLL_I,	ACRO_RATE_ROLL_D,	ACRO_RATE_ROLL_IMAX * 100),
		pid_acro_rate_pitch	(k_param_pid_acro_rate_pitch,	PSTR("ACR_PIT_"),	ACRO_RATE_PITCH_P,  ACRO_RATE_PITCH_I,	ACRO_RATE_PITCH_D,	ACRO_RATE_PITCH_IMAX * 100),
		pid_acro_rate_yaw	(k_param_pid_acro_rate_yaw,		PSTR("ACR_YAW_"),	ACRO_RATE_YAW_P,    ACRO_RATE_YAW_I,	ACRO_RATE_YAW_D,	ACRO_RATE_YAW_IMAX * 100),

		pid_stabilize_roll	(k_param_pid_stabilize_roll,	PSTR("STB_RLL_"),	STABILIZE_ROLL_P,   STABILIZE_ROLL_I,	STABILIZE_ROLL_D,	STABILIZE_ROLL_IMAX * 100),
		pid_stabilize_pitch	(k_param_pid_stabilize_pitch,	PSTR("STB_PIT_"),	STABILIZE_PITCH_P,  STABILIZE_PITCH_I,	STABILIZE_PITCH_D,  STABILIZE_PITCH_IMAX * 100),
		pid_yaw				(k_param_pid_yaw,				PSTR("STB_YAW_"),	YAW_P,      		YAW_I,				YAW_D,				YAW_IMAX * 100),

		pid_nav_lat			(k_param_pid_nav_lat,			PSTR("NAV_LAT_"),	NAV_LOITER_P,		NAV_LOITER_I,		NAV_LOITER_D,		NAV_LOITER_IMAX * 100),
		pid_nav_lon			(k_param_pid_nav_lon,			PSTR("NAV_LON_"),	NAV_LOITER_P,      	NAV_LOITER_I,		NAV_LOITER_D,		NAV_LOITER_IMAX * 100),
		pid_nav_wp			(k_param_pid_nav_wp,			PSTR("NAV_WP_"),	NAV_WP_P,      		NAV_WP_I,			NAV_WP_D,			NAV_WP_IMAX * 100),

		pid_baro_throttle	(k_param_pid_baro_throttle,		PSTR("THR_BAR_"),	THROTTLE_BARO_P,    THROTTLE_BARO_I,	THROTTLE_BARO_D,	THROTTLE_BARO_IMAX),
		pid_sonar_throttle	(k_param_pid_sonar_throttle,	PSTR("THR_SON_"),	THROTTLE_SONAR_P,   THROTTLE_SONAR_I,	THROTTLE_SONAR_D,	THROTTLE_SONAR_IMAX),

        junk(0)     // XXX just so that we can add things without worrying about the trailing comma
    {
    }
};

#endif // PARAMETERS_H
