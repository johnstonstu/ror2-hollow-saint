"""Original Hollow Saint sculptural construction v2; isolated background only."""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
from refine_helpers import *

if not bpy.app.background:
    raise RuntimeError('Use --background --factory-startup to preserve live work.')
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art'/'refinement'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version = 0
ivory_color = (.76, .69, .565)
ivory = material('Porcelain | warm ivory satin', ivory_color, .06, .48)
dark = material('Graphite | joint and inner chassis', (.014, .018, .021), .18, .58)
edge = material('Ceramic fracture recess', (.21, .19, .155), .05, .8)
copper = material('Copper | satin aged metal', (.39, .155, .063), .62, .43)
copper_edge = material('Copper | polished edge', (.58, .32, .145), .75, .32)
cyan = material('Quiet cyan conductor', (.015, .66, .82), .15, .26, 2)
white = material('Core cyan white', (.25, .92, 1), .0, .24, 4)
cloth = material('Woven charcoal tabard', (.055, .05, .045), .0, .96)

# Small head and sculptural short neck; an unbroken blank mask with recessed seam.
loft('Neck | flared sinew', [(0,.018,1.635,.064,.052),(0,.024,1.663,.06,.045),
                           (0,.03,1.694,.041,.039),(0,.035,1.745,.03,.035),
                           (0,.035,1.77,.033,.039),(0,.025,1.815,.05,.049)],dark,16)
mask_rings = [(0,-.037,1.755,.012,.025),(0,-.031,1.78,.029,.043),
              (0,-.015,1.825,.05,.069),(0,-.006,1.875,.072,.092),
              (0,.004,1.925,.078,.093),(0,.009,1.968,.064,.072),
              (0,.014,1.992,.039,.045),(0,.015,2.001,.008,.013)]
mask = loft('Mask | tapered blank porcelain',mask_rings,ivory,24,True)
facet_materials(mask,ivory_color)
rod('Mask | dark incised center',[(0,y-d-.0009,z) for x,y,z,w,d in mask_rings],.0048,dark)
rod('Mask | narrow cyan inlay',[(0,y-d-.006,z) for x,y,z,w,d in mask_rings[1:-1]],.0028,cyan)
rod('Throat | cyan filament',[(0,-.031,1.67),(0,-.025,1.706),(0,-.028,1.75)],.004,cyan)
for s in [-1,1]:
    sphere(('L' if s>0 else 'R')+' mask ear hinge',(s*.05,.034,1.823),(.013,.019,.023),dark)
    rod('Neck diagonal tendon '+str(s),[(s*.026,.042,1.785),(s*.033,.042,1.703),(s*.059,.046,1.64)],.01,dark)

loft('Torso | shaped graphite understructure',[(0,0,1.07,.112,.075),(0,0,1.18,.083,.06),
    (0,0,1.31,.096,.068),(0,0,1.42,.147,.088),(0,0,1.53,.188,.103),
    (0,.007,1.62,.16,.078),(0,.01,1.667,.098,.055)],dark,16)
sphere('Pelvis | compact inner cradle',(0,0,1.077),(.133,.079,.1),dark)

# Four overlapping abdomen plates give organic articulation instead of a cone.
for i,(z,w) in enumerate([(1.355,.099),(1.29,.084),(1.225,.069),(1.16,.057)]):
    shield('Abdomen | overlapping chevron '+str(i),[(-w,-.063,z+.035),(-w*.87,-.078,z-.018),
        (0,-.085,z-.06),(w*.87,-.078,z-.018),(w,-.063,z+.035),(0,-.085,z+.014)],.037,dark,.013)
    if i<2:
        rod('Abdomen | cyan inset '+str(i),[(0,-.104,z+.003),(0,-.104,z-.025)],.004,cyan)

for s, side in [(1,'L'),(-1,'R')]:
    def p(x,y,z): return (s*x,y,z)
    # Curving C-shaped pectoral petals reveal the recessed sternum core.
    chest = ribbon(side+' chest | crescent porcelain',[
        (1.654,.028,.125,-.069),(1.62,.042,.167,-.089),(1.58,.075,.19,-.112),
        (1.54,.102,.184,-.126),(1.50,.103,.17,-.13),(1.457,.084,.146,-.116),
        (1.425,.074,.103,-.10)],s,ivory)
    facet_materials(chest,ivory_color)
    shield(side+' oblique | narrow ivory rib',[p(x,y,z) for x,y,z in [
        (.14,-.05,1.414),(.115,-.066,1.36),(.077,-.068,1.285),(.069,-.084,1.341),(.092,-.088,1.4)]],.042,ivory,.009)
    rod(side+' thorax cyan slash',[p(.071,-.115,1.429),p(.098,-.098,1.399)],.004,cyan)
    sphere(side+' shoulder | ball',p(.22,.013,1.578),(.045,.05,.048),dark)
    shoulder=loft(side+' shoulder | swept ceramic leaf',[
        p(.284,-.019,1.51)+(.012,.038),p(.264,-.012,1.548)+(.042,.061),
        p(.243,.003,1.595)+(.063,.073),p(.224,.008,1.638)+(.041,.056),
        p(.194,.012,1.68)+(.006,.012)],ivory,12)
    facet_materials(shoulder,ivory_color)
    # Long dark upper arm has a convex bicep and narrowing elbow tendon.
    loft(side+' arm | upper sinew',[p(.246,0,1.56)+(.042,.044),p(.27,-.005,1.51)+(.049,.045),
        p(.298,-.011,1.445)+(.038,.039),p(.333,-.015,1.361)+(.022,.026)],dark)
    elbow=p(.337,-.015,1.335)
    sphere(side+' elbow | flexion joint',elbow,(.037,.038,.051),dark)
    sphere(side+' elbow | cyan inset',p(.337,-.052,1.34),(.009,.006,.02),cyan)
    bone(side+' forearm | chassis',p(.342,-.009,1.32),p(.425,-.025,1.085),.025,dark)
    forearm=loft(side+' forearm | pointed ceramic vambrace',[
        p(.425,-.029,1.091)+(.024,.028),p(.41,-.027,1.14)+(.031,.035),
        p(.385,-.019,1.22)+(.048,.047),p(.365,-.012,1.292)+(.055,.045),
        p(.371,-.005,1.345)+(.028,.029)],ivory,12)
    for vertex in list(forearm.data.vertices)[-12:]:
        vertex.co.z+=.035*math.sin((vertex.index%12)*math.tau/12)
    facet_materials(forearm,ivory_color)
    # Empty cutout appearance at the elbow is created by a dark overlapping joint.
    sphere(side+' wrist',p(.43,-.026,1.069),(.025,.026,.028),dark)
    sphere(side+' wrist | cyan mark',p(.43,-.052,1.074),(.007,.004,.012),cyan)
    loft(side+' hand | shaped palm',[p(.445,-.032,.98)+(.027,.018),
        p(.443,-.028,1.021)+(.035,.022),p(.432,-.026,1.053)+(.023,.022)],dark,10)
    for digit,dx,length in [('index',-.024,.088),('middle',-.006,.103),('ring',.013,.096),('little',.03,.077)]:
        a=p(.444+dx,-.032,.989)
        b=p(.454+dx*1.65,-.034,.989-length*.45)
        c=p(.458+dx*2.0,-.061,.989-length*.82)
        d=p(.449+dx*2.02,-.093-(.012 if digit in ['ring','little'] else 0),.989-length*.84)
        for j,(u,v,r) in enumerate([(a,b,.010),(b,c,.009),(c,d,.0075)]):
            bone(side+' '+digit+' | phalanx '+str(j+1),u,v,r,dark)
        sphere(side+' '+digit+' knuckle',a,(.011,.012,.012),dark,1)
    for j,(a,b,r) in enumerate([(p(.415,-.026,1.019),p(.393,-.042,.987),.013),
                                (p(.393,-.042,.987),p(.396,-.067,.959),.011)]):
        bone(side+' thumb | phalanx '+str(j+1),a,b,r,dark)
    # Hip crescent and long, narrow ceramic thigh armor.
    shield(side+' hip | ceramic crest',[p(x,y,z) for x,y,z in [
        (.026,-.081,1.117),(.112,-.058,1.166),(.159,-.022,1.13),(.139,-.071,1.09),(.05,-.096,1.084)]],.036,ivory,.01)
    hip,knee,ankle=p(.112,0,1.07),p(.19,.005,.631),p(.238,.015,.135)
    sphere(side+' hip socket',hip,(.066,.065,.074),dark)
    loft(side+' thigh | inner muscle',[p(.12,0,1.062)+(.06,.063),p(.144,0,.966)+(.063,.063),
        p(.169,0,.811)+(.048,.05),p(.187,.003,.67)+(.033,.041)],dark)
    thigh=loft(side+' thigh | tapered porcelain shell',[p(.185,-.026,.713)+(.031,.032),
        p(.178,-.029,.77)+(.044,.038),p(.155,-.034,.924)+(.055,.05),
        p(.137,-.049,1.044)+(.042,.039),p(.139,-.067,1.083)+(.016,.025)],ivory,12)
    for vertex in list(thigh.data.vertices)[:12]:
        vertex.co.z+=.026*math.sin((vertex.index%12)*math.tau/12)
    facet_materials(thigh,ivory_color)
    sphere(side+' knee | graphite articulation',knee,(.036,.044,.052),dark)
    sphere(side+' knee | cyan slit',p(.19,-.04,.638),(.007,.006,.019),cyan)
    bone(side+' calf | dark rear sinew',p(.195,.024,.599),ankle,.032,dark,end=.023)
    shin=loft(side+' shin | contoured porcelain greave',[
        p(.238,.002,.145)+(.024,.029),p(.233,-.002,.211)+(.026,.031),
        p(.22,-.003,.338)+(.032,.035),p(.208,-.006,.453)+(.052,.052),
        p(.203,-.008,.535)+(.059,.052),p(.195,-.008,.585)+(.037,.035)],ivory,12)
    for vertex in list(shin.data.vertices)[-12:]:
        vertex.co.z+=.035*math.sin((vertex.index%12)*math.tau/12)
    facet_materials(shin,ivory_color)
    sphere(side+' ankle joint',ankle,(.033,.046,.035),dark)
    sphere(side+' foot | recessed dark sole',p(.244,-.035,.042),(.04,.096,.028),dark)
    toe(side+' foot | inner porcelain toe',.215,ivory,s)
    toe(side+' foot | outer porcelain toe',.276,ivory,s)
    loft(side+' foot | arched instep bridge',[p(.245,-.026,.083)+(.019,.039),
        p(.243,-.011,.117)+(.033,.045),p(.239,.006,.155)+(.02,.027)],ivory,12)
    sphere(side+' ankle | side cyan pin',p(.27,.015,.135),(.004,.012,.012),cyan)
    # Quiet rear shoulder blade makes the back useful at play distance.
    shield(side+' back | porcelain scapula',[p(x,y,z) for x,y,z in [
        (.047,.092,1.644),(.137,.085,1.633),(.199,.055,1.568),(.15,.102,1.473),(.079,.11,1.49)]],-.047,ivory,-.01)
    rod(side+' chest | hairline fracture',[p(.157,-.104,1.596),p(.134,-.119,1.581),p(.14,-.123,1.56)],.0007,edge)
    rod(side+' thigh | hairline fracture',[p(.173,-.067,.901),p(.154,-.075,.882),p(.162,-.073,.864)],.0006,edge)

# Small recessed core: its outer dark housing is deliberately wider than the light.
disc('Sternum | graphite recessed housing',(0,-.105,1.526),.075,.039,dark)
disc('Sternum | copper inner rim',(0,-.128,1.526),.052,.005,copper_edge)
disc('Sternum | cyan lens',(0,-.132,1.526),.047,.006,cyan)
disc('Sternum | luminous center',(0,-.137,1.526),.029,.006,white)
disc('Back | small charge node',(0,.111,1.522),.026,.018,cyan)
rod('Back | spine filament',[(0,.081,1.221),(0,.092,1.41),(0,.112,1.491)],.0035,cyan)
from refine_fit import fit
fit(PARTS)
from refine_accessories import accessories
accessories(ivory,dark,copper,copper_edge,cyan,cloth)
from refine_presentation import present
present(ROOT, OUT, PARTS)
