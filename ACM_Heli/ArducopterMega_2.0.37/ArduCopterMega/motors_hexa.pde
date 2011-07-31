/// -*- tab-width: 4; Mode: C++; c-basic-offset: 4; indent-tabs-mode: nil -*-

#if FRAME_CONFIG ==	HEXA_FRAME

static void output_motors_armed()
{
	int roll_out, pitch_out;
	int out_min = g.rc_3.radio_min;
	int out_max = g.rc_3.radio_max;

	// Throttle is 0 to 1000 only
	g.rc_3.servo_out 	= constrain(g.rc_3.servo_out, 0, 1000);

	if(g.rc_3.servo_out > 0)
		out_min = g.rc_3.radio_min + MINIMUM_THROTTLE;

	g.rc_1.calc_pwm();
	g.rc_2.calc_pwm();
	g.rc_3.calc_pwm();
	g.rc_4.calc_pwm();

	if(g.frame_orientation == X_FRAME){
		roll_out 	 	= g.rc_1.pwm_out / 2;
		pitch_out 	 	= (float)g.rc_2.pwm_out * .866;

		//left side
		motor_out[CH_2]		= g.rc_3.radio_out + g.rc_1.pwm_out;		// CCW Middle
		motor_out[CH_3]		= g.rc_3.radio_out + roll_out + pitch_out;	// CW Front
		motor_out[CH_8]     = g.rc_3.radio_out + roll_out - pitch_out;	// CW Back

		//right side
		motor_out[CH_1]		= g.rc_3.radio_out - g.rc_1.pwm_out;		// CW Middle
		motor_out[CH_7] 	= g.rc_3.radio_out - roll_out + pitch_out;	// CCW Front
		motor_out[CH_4] 	= g.rc_3.radio_out - roll_out - pitch_out;	// CCW Back

	}else{
		roll_out 	 	= (float)g.rc_1.pwm_out * .866;
		pitch_out 	 	= g.rc_2.pwm_out / 2;

		//Front side
		motor_out[CH_1]		= g.rc_3.radio_out + g.rc_2.pwm_out;		// CW	 FRONT
		motor_out[CH_7] 	= g.rc_3.radio_out + roll_out + pitch_out;	// CCW	 FRONT LEFT
		motor_out[CH_4] 	= g.rc_3.radio_out - roll_out + pitch_out;	// CCW	 FRONT RIGHT

		//Back side
		motor_out[CH_2]		= g.rc_3.radio_out - g.rc_2.pwm_out;		// CCW	BACK
		motor_out[CH_3]		= g.rc_3.radio_out + roll_out - pitch_out;	// CW, 	BACK LEFT
		motor_out[CH_8]		= g.rc_3.radio_out - roll_out - pitch_out;	// CW	BACK RIGHT
	}

	// Yaw
	motor_out[CH_2]		+= g.rc_4.pwm_out;	// CCW
	motor_out[CH_7]		+= g.rc_4.pwm_out;	// CCW
	motor_out[CH_4] 	+= g.rc_4.pwm_out;	// CCW

	motor_out[CH_3]		-= g.rc_4.pwm_out;	// CW
	motor_out[CH_1]		-= g.rc_4.pwm_out;	// CW
	motor_out[CH_8]		-= g.rc_4.pwm_out;  // CW

	// limit output so motors don't stop
	motor_out[CH_1] = max(motor_out[CH_1],  out_min);
	motor_out[CH_2] = max(motor_out[CH_2],  out_min);
	motor_out[CH_3] = max(motor_out[CH_3],  out_min);
	motor_out[CH_4] = max(motor_out[CH_4],  out_min);
	motor_out[CH_7] = max(motor_out[CH_7],  out_min);
	motor_out[CH_8] = max(motor_out[CH_8],  out_min);

	motor_out[CH_1]	= min(motor_out[CH_1], 	out_max);
	motor_out[CH_2]	= min(motor_out[CH_2], 	out_max);
	motor_out[CH_3]	= min(motor_out[CH_3], 	out_max);
	motor_out[CH_4] = min(motor_out[CH_4], 	out_max);
	motor_out[CH_7] = min(motor_out[CH_7],  out_max);
	motor_out[CH_8]	= min(motor_out[CH_8],  out_max);


	#if CUT_MOTORS == ENABLED
	// Send commands to motors
	if(g.rc_3.servo_out > 0){
		APM_RC.OutputCh(CH_1, motor_out[CH_1]);
		APM_RC.OutputCh(CH_2, motor_out[CH_2]);
		APM_RC.OutputCh(CH_3, motor_out[CH_3]);
		APM_RC.OutputCh(CH_4, motor_out[CH_4]);
		APM_RC.OutputCh(CH_7, motor_out[CH_7]);
		APM_RC.OutputCh(CH_8, motor_out[CH_8]);

		// InstantPWM
		APM_RC.Force_Out0_Out1();
		APM_RC.Force_Out6_Out7();
		APM_RC.Force_Out2_Out3();
	}else{
		APM_RC.OutputCh(CH_1, g.rc_3.radio_min);
		APM_RC.OutputCh(CH_2, g.rc_3.radio_min);
		APM_RC.OutputCh(CH_3, g.rc_3.radio_min);
		APM_RC.OutputCh(CH_4, g.rc_3.radio_min);
		APM_RC.OutputCh(CH_7, g.rc_3.radio_min);
		APM_RC.OutputCh(CH_8, g.rc_3.radio_min);
	}

	#else

		APM_RC.OutputCh(CH_1, motor_out[CH_1]);
		APM_RC.OutputCh(CH_2, motor_out[CH_2]);
		APM_RC.OutputCh(CH_3, motor_out[CH_3]);
		APM_RC.OutputCh(CH_4, motor_out[CH_4]);
		APM_RC.OutputCh(CH_7, motor_out[CH_7]);
		APM_RC.OutputCh(CH_8, motor_out[CH_8]);

		// InstantPWM
		APM_RC.Force_Out0_Out1();
		APM_RC.Force_Out6_Out7();
		APM_RC.Force_Out2_Out3();
	#endif
}

static void output_motors_disarmed()
{
	if(g.rc_3.control_in > 0){
		// we have pushed up the throttle
		// remove safety
		motor_auto_armed = true;
	}

	// fill the motor_out[] array for HIL use
	for (unsigned char i = 0; i < 8; i++) {
		motor_out[i] = g.rc_3.radio_min;
	}

	// Send commands to motors
	APM_RC.OutputCh(CH_1, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_2, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_3, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_4, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_7, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_8, g.rc_3.radio_min);
}

static void output_motor_test()
{
	motor_out[CH_1] = g.rc_3.radio_min;
	motor_out[CH_2] = g.rc_3.radio_min;
	motor_out[CH_3] = g.rc_3.radio_min;
	motor_out[CH_4] = g.rc_3.radio_min;
	motor_out[CH_7] = g.rc_3.radio_min;
	motor_out[CH_8] = g.rc_3.radio_min;


	if(g.frame_orientation == X_FRAME){
//  31
//	24
		if(g.rc_1.control_in > 3000){	// right
			motor_out[CH_1] += 50;
		}

		if(g.rc_1.control_in < -3000){	// left
			motor_out[CH_2] += 50;
		}

		if(g.rc_2.control_in > 3000){ 	// back
			motor_out[CH_8] += 50;
			motor_out[CH_4] += 50;
		}

		if(g.rc_2.control_in < -3000){	// front
			motor_out[CH_7] += 50;
			motor_out[CH_3] += 50;
		}

	}else{
//  3
// 2 1
//	4
		if(g.rc_1.control_in > 3000){	// right
			motor_out[CH_4] += 50;
			motor_out[CH_8] += 50;
		}

		if(g.rc_1.control_in < -3000){	// left
			motor_out[CH_7] += 50;
			motor_out[CH_3] += 50;
		}

		if(g.rc_2.control_in > 3000){ 	// back
			motor_out[CH_2] += 50;
		}

		if(g.rc_2.control_in < -3000){	// front
			motor_out[CH_1] += 50;
		}

	}

	APM_RC.OutputCh(CH_1, motor_out[CH_1]);
	APM_RC.OutputCh(CH_2, motor_out[CH_2]);
	APM_RC.OutputCh(CH_3, motor_out[CH_3]);
	APM_RC.OutputCh(CH_4, motor_out[CH_4]);
	APM_RC.OutputCh(CH_7, motor_out[CH_7]);
	APM_RC.OutputCh(CH_8, motor_out[CH_8]);
}

/*
	APM_RC.OutputCh(CH_2, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_3, g.rc_3.radio_min + 50);
	delay(1000);

	APM_RC.OutputCh(CH_3, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_7, g.rc_3.radio_min + 50);
	delay(1000);

	APM_RC.OutputCh(CH_7, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_1, g.rc_3.radio_min + 50);
	delay(1000);

	APM_RC.OutputCh(CH_1, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_4, g.rc_3.radio_min + 50);
	delay(1000);

	APM_RC.OutputCh(CH_4, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_8, g.rc_3.radio_min + 50);
	delay(1000);

	APM_RC.OutputCh(CH_8, g.rc_3.radio_min);
	APM_RC.OutputCh(CH_2, g.rc_3.radio_min + 50);
	delay(1000);
}
*/

#endif