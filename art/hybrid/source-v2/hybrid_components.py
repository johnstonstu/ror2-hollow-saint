"""Original replacement components fitted to the HF starter's measured envelope."""
import math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from refine_helpers import mesh,rod,bone,loft,sphere,ribbon,disc


def halo(cx,copper,edge,cyan,dark):
    center_z,y,inner,outer=1.79,.15,.320,.433
    intervals=[(11,70),(77,136),(224,283),(290,349)]
    for index,(low,high) in enumerate(intervals):
        lo,hi=math.radians(low),math.radians(high)
        steps=24
        verts=[]
        for i in range(steps+1):
            angle=lo+(hi-lo)*i/steps
            for r,yy in [(inner,y-.018),(outer,y-.018),(outer,y+.018),(inner,y+.018)]:
                verts.append((cx+r*math.sin(angle),yy,center_z+r*math.cos(angle)))
        faces=[(3,2,1,0)]
        for i in range(steps):
            for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
        faces.append(tuple(range(steps*4,(steps+1)*4)))
        mesh('HALO | independent copper arc '+str(index+1),verts,faces,copper,.002)
        for title,r in [('outer',outer-.003),('inner',inner+.003)]:
            rod('HALO '+str(index+1)+' | '+title+' lip',[
                (cx+r*math.sin(lo+(hi-lo)*i/steps),y-.019,center_z+r*math.cos(lo+(hi-lo)*i/steps)) for i in range(steps+1)],.0012,edge)
    for angle in [73.5,286.5]:
        a=math.radians(angle)
        r=.3765
        x,z=cx+r*math.sin(a),center_z+r*math.cos(a)
        dx,dz=math.cos(a)*.017,-math.sin(a)*.017
        rod('HALO | geometric gap conductor '+str(angle),[(x-dx,y,z-dz),(x+dx,y,z+dz)],.003,cyan)
    # Compact forked links follow the upper back; no visible full-width crossbar.
    for s in [-1,1]:
        rod('YOKE | fitted branch '+str(s),[(cx+s*.024,.119,1.58),(cx+s*.123,.137,1.535),(cx+s*.274,y,1.515)],.010,dark)


def hands(dark,cyan):
    for s,side,wx in [(1,'L',.475),(-1,'R',-.516)]:
        def p(x,y,z):return (wx+s*x,y,z)
        loft(side+' HAND | wrist cuff',[p(0,-.049,.995)+(.037,.057),
            p(.004,-.053,.957)+(.033,.045),p(.009,-.056,.925)+(.039,.036)],dark,12)
        loft(side+' HAND | tapered sculpted palm',[p(.008,-.055,.94)+(.035,.035),
            p(.015,-.057,.895)+(.053,.031),p(.012,-.06,.855)+(.053,.027),
            p(.013,-.061,.842)+(.044,.023)],dark,12)
        for digit,dx,length,fan,curl in [('index',-.035,.167,-.015,.042),
                                        ('middle',-.010,.190,-.003,.05),
                                        ('ring',.018,.177,.018,.066),
                                        ('little',.043,.148,.028,.084)]:
            a=p(.012+dx,-.059,.865)
            b=p(.013+dx+fan*.5,-.060,.865-length*.43)
            c=p(.014+dx+fan,-.076,.865-length*.81)
            d=p(.009+dx+fan,-.076-curl,.865-length*.93)
            for n,(u,v,r) in enumerate([(a,b,.016),(b,c,.013),(c,d,.0095)]):
                bone(side+' HAND | '+digit+' segment '+str(n+1),u,v,r,dark,end=r*.78)
            sphere(side+' HAND | '+digit+' knuckle',a,(.017,.017,.019),dark,2)
        thumb=[p(-.022,-.052,.915),p(-.064,-.065,.882),p(-.09,-.081,.845),p(-.08,-.119,.821)]
        for i,(a,b,r) in enumerate(zip(thumb,thumb[1:],[.020,.017,.012])):
            bone(side+' HAND | thumb segment '+str(i+1),a,b,r,dark)
        sphere(side+' HAND | small wrist conductor',p(.003,-.106,.973),(.009,.004,.016),cyan)


def back_details(cx,dark,ivory,cyan,body):
    # Shells sit at the corrected back envelope and taper into its anatomy.
    for s,side in [(1,'L'),(-1,'R')]:
        rows=[(1.668,.039,.085,.104),(1.636,.038,.14,.108),
              (1.59,.056,.161,.09),(1.549,.076,.138,.071),(1.523,.094,.111,.059)]
        obj=ribbon(side+' BACK | small scapula shell',rows,s,ivory)
        for v in obj.data.vertices:
            v.co.x+=cx
            v.co.y=.207-v.co.y
    disc('BACK | small recessed charge node',(cx,.126,1.55),.024,.008,cyan)
    vertices=[v.co.copy() for v in body.data.vertices]
    tree=BVHTree.FromPolygons(vertices,[tuple(p.vertices) for p in body.data.polygons])
    line=[]
    for i in range(25):
        z=1.31+i*(1.522-1.31)/24
        hit,*_=tree.ray_cast(Vector((cx,1,z)),Vector((0,-1,0)))
        if hit is None:raise RuntimeError('Back conductor ray missed body')
        line.append((cx,hit.y+.0015,z))
    rod('BACK | seated spine conductor',line,.003,cyan)
