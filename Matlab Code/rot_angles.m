function [theta_degrees,phi_degrees ] = rot_angles(X,Y,Z)

    ZD_axis_offset = 76.7038;
    
    %Z = [ZD_axis_offset - D(1,1), ZD_axis_offset - D(1,2), ZD_axis_offset - D(1,3), ZD_axis_offset - D(1,4)];

    A_Plane = plane_fit(X,Y,Z);
    
    A_Plane_Vector = [-A_Plane(2); -A_Plane(3); 1];

    %rotation around y

    theta = -1*(atan(A_Plane_Vector(1,1)/A_Plane_Vector(3,1))); 
    theta_degrees = theta *(180/pi);

    rot_y = [cos(theta),  0,    sin(theta);
             0,           1,            0 ;
             -sin(theta)  0,    cos(theta);];

    A_Plane_Vector_2 = rot_y*A_Plane_Vector; 

    %rotation around x

    phi = (atan(A_Plane_Vector_2(2,1)/A_Plane_Vector_2(3,1))); 
    phi_degrees = phi * (180/pi);
    
    rot_x = [ 1,        0,         0;
              0, cos(phi), -sin(phi);
              0, sin(phi),  cos(phi)];

    A_Plane_Vector_3 = rot_x * A_Plane_Vector_2;    
    
    save('coords.mat','X','Y','Z')   

end

