"""Reviewed physical refinements beyond the decorative draft: true arch apertures."""
import math

def add_station_refinements(b):
    b.set_module('Module_Station_ArchJoinery')
    for z in (-6.,6.):
        sign=1 if z>0 else -1
        for x in (-12.,-8.,-4.,4.,8.,12.):
            # The old rectangle's upper corners become masonry on BOTH sides of the same wall.
            for i in range(32):
                a=-1.1+2.2*i/32;c=-1.1+2.2*(i+1)/32
                ya=3+math.sqrt(max(0,1.21-a*a));yc=3+math.sqrt(max(0,1.21-c*c))
                points=[(x+a,ya,z-.12),(x+c,yc,z-.12),(x+c,4.1,z-.12),(x+a,4.1,z-.12)]
                points += [(xx,yy,z+.12) for xx,yy,_ in points]
                b.authored_mesh('ArchStoneCorner_%s_%s_%s'%(x,z,i),points,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'RailStone')
            for i in range(24):
                a=i*math.pi/24;c=(i+1)*math.pi/24
                b.beam('ArchedGlazingBead_%s_%s_%s'%(x,z,i),(x+1.08*math.cos(a),3+1.08*math.sin(a),z+sign*.135),
                       (x+1.08*math.cos(c),3+1.08*math.sin(c),z+sign*.135),.024,'RailGreen',vertices=8)
    b.box('WasteBinLabelPlate',(-11.3,.41,-1.045),(.18,.10,.012),'RailIron',bevel=.002)
