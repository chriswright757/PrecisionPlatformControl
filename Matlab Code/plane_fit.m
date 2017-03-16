function Plane = plane_fit(X,Y,Z)

%calc sums and sums of squares and sums of products

    n = length(X);
    SX = sum(X);
    SY = sum(Y);
    SX2 = sum(X.^2);
    SY2 = sum(Y.^2);
    SXY = sum(X.*Y);
    SZ = sum(Z);
    SXZ = sum(X.*Z);
    SYZ = sum(Y.*Z);

    % calc plane coords by least squares
    % A is in the form of:
    % Z = A(1) + A(2)X + A(3)Y
        
        Plane = [n,SX,SY;
               SX,SX2,SXY;
               SY,SXY,SY2]\[SZ;SXZ;SYZ];      

end 