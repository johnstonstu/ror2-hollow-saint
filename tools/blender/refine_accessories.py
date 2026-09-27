"""Segmented metal halo and folded long ceremonial cloth."""
import math
from refine_helpers import mesh, rod, bone, sphere


def accessories(ivory, dark, copper, copper_edge, cyan, cloth):
    for name,y,back in [('Front',-.11,False),('Back',.101,True)]:
        # Separate offset strips: broad center fold, unequal hems and restrained trim.
        for panel,(xl,xr,bottom) in enumerate([(-.1,-.019,.465),(-.02,.036,.395),(.035,.097,.477)]):
            rows=[(1.202,0),(1.1,.005),(.87,-.006),(.63,.009),(bottom,.024)]
            vertices=[]
            for z,fold in rows:
                yy=y+fold*(1 if back else -1)
                tip=(1.202-z)/.807
                shift=.009*math.sin(tip*3.5+panel)
                taper=1-.14*tip
                vertices += [(xl*taper+shift,yy,z),((xl+xr)/2+shift,yy+(.011 if back else -.011),z-.017*tip),(xr*taper+shift,yy,z+.014*tip)]
            faces=[]
            for j in range(4):
                for i in range(2):
                    faces.append((j*3+i,j*3+i+1,(j+1)*3+i+1,(j+1)*3+i))
            obj=mesh(name+' tabard | folded panel '+str(panel+1),vertices,faces,cloth)
            mod=obj.modifiers.new('Thin woven fabric thickness','SOLIDIFY')
            mod.thickness=.003
            if panel in (0,2):
                x=xl if panel==0 else xr
                rod(name+' tabard | copper selvedge '+str(panel),
                    [(x*(1-.14*(1.202-z)/.807)+.009*math.sin((1.202-z)/.807*3.5+panel),yy+(.002 if back else -.002),z+(.014*(1.202-z)/.807 if panel==2 else 0)) for z,fold in rows
                     for yy in [y+fold*(1 if back else -1)]],.0018,copper_edge)
        motif_y=y+(.019 if back else -.019)
        rod(name+' tabard | conductor sigil line',[(0,motif_y,1.008),(0,motif_y,.731)],.0017,copper_edge)
        rod(name+' tabard | conductor sigil ring',[(.037*math.sin(i*math.tau/48),motif_y,.872+.037*math.cos(i*math.tau/48))
                                                        for i in range(49)],.0017,copper_edge)

    # Four separate arcs, truncated lower sections follow selected LEFT A's open bottom.
    center_z, center_y, outer, inner = 1.775, .20, .427, .332
    for segment in range(4):
        lo,hi=math.radians(segment*90+4),math.radians((segment+1)*90-4)
        if segment==1: hi=math.radians(137)
        if segment==2: lo=math.radians(223)
        steps=32
        verts=[]
        for i in range(steps+1):
            theta=lo+(hi-lo)*i/steps
            for r,y in [(inner,center_y-.018),(outer,center_y-.018),
                        (outer,center_y+.018),(inner,center_y+.018)]:
                verts.append((r*math.sin(theta),y,center_z+r*math.cos(theta)))
        faces=[(3,2,1,0)]
        for i in range(steps):
            for j in range(4):
                faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
        faces.append(tuple(range(steps*4,(steps+1)*4)))
        obj=mesh('Halo | copper quadrant '+str(segment+1),verts,faces,copper,.002)
        for edge,r in [('outer',outer-.003),('inner',inner+.003)]:
            rod('Halo quadrant '+str(segment+1)+' | '+edge+' metal bevel highlight',
                [(r*math.sin(lo+(hi-lo)*i/steps),center_y-.0195,
                  center_z+r*math.cos(lo+(hi-lo)*i/steps)) for i in range(steps+1)],.0013,copper_edge)
        for notch in [.18,.54,.84]:
            theta=lo+(hi-lo)*notch
            rod('Halo quadrant '+str(segment+1)+' | shallow radial score '+str(notch),
                [(r*math.sin(theta),center_y-.019,center_z+r*math.cos(theta))
                 for r in [outer-.003,outer-.017]],.0008,dark)
    for i in [0,1,3]:
        theta=math.radians(i*90)
        r=(inner+outer)/2
        x,z=r*math.sin(theta),center_z+r*math.cos(theta)
        dx,dz=math.cos(theta)*.024,-math.sin(theta)*.024
        rod('Halo | restrained cyan gap '+str(i+1),[(x-dx,center_y,z-dz),(x+dx,center_y,z+dz)],.003,cyan)
    for s in [-1,1]:
        bone('Halo yoke | spine link '+str(s),(s*.026,.114,1.57),(s*.15,.164,1.6),.016,dark)
        bone('Halo yoke | ring link '+str(s),(s*.15,.164,1.6),(s*.295,center_y,1.595),.016,dark)
