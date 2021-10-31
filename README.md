# PrecisionPlatformControl

This software was developed to run the ultrafast ultra-precision platform at the University of Cambridge. 

The thesis that this software was used for can be found at https://doi.org/10.17863/CAM.70795

This was my first major use of C# and therefore a lot of lessons were learnt along the way and if I had the time and knowledge I have now there would be significant improvements to the structure and methods used. The code is certainly disjointed in certain areas as different experiments were undertaken and limited time meant they couldn't be fully integrated into the main branch of the software. 

## Functions

* Aerotech stage control 
* Control of laser parameters
* Microscope control
* In-process power logging 
* Autofocus for microscope
* Laser Alignment 
* Autofocus of laser
* Sample tilt correction
* Microscope-Laser alignment 
* Machining on an angled plane 

## Initialisation Window

<p align="center">
    <img src="Readme Images\Initialisation_Form.png" alt="Select save directory" width="800"/>
</p>

## Main Operation Window

<p align="center">
    <img src="Readme Images\Interaction_Form.png" alt="Select save directory" width="800"/>
</p>

## Image Processing Window 

<p align="center">
    <img src="Readme Images\Image_Processing_Form.png" alt="Select save directory" width="800"/>
</p>

## System Devices & Interfaces

|Component |	Function	|Interface|
|----------|----------------|---------|
|Aerotech 5 axis Stage	|Stage Movement 	|Aerobasic Software, TCP/IP, API (using DLL)|
|Talisker Ultra	|Laser Source, Power Control, Repetition Rate, Shutter, AOM contro|Talisker Control Software, RS232 (serial), <br />Pulse Picking using 5-volt input|
|Beam Expander	|Beam Diameter	|Linos Software, RS232 (serial)|
|Watt Pilot	|Power Control	|Watt Pilot Software RS232 (serial)|
|Spiricon SP300	|Record Beam Profile	|Beamgauge Software, API (using DLL)|
|Ophir Power Meters	|Record measured beam power	|Starlab Software, API (using DLL)|
|Microscope	|Capture Images, Illumination Control, Zoom Control,|	For Camera - ICMeasure Software, API (using DLL) <br /> For Zoom lens – RS232(serial) <br />For Illumination – RS232 (serial)|



