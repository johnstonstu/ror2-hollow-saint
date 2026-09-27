"""Reproducible Hollow Saint blockout. Run in isolated Blender --background --factory-startup.

Construction proportions are proposals for visual review, not an approved rig or game asset.
The live interactive Blender scene is never accessed by this script.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

if not bpy.app.background:
    raise RuntimeError('Run in an isolated --background --factory-startup process; live scenes must be preserved.')

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "art" / "blockout"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.render.engine = 'CYCLES'
scene.cycles.samples = 40
scene.cycles.use_denoising = True
scene.render.resolution_x = 1000
scene.render.resolution_y = 1200
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world.color = (0.35, 0.35, 0.35)
scene.view_settings.view_transform = 'AgX'
scene.render.film_transparent = False
bpy.context.preferences.filepaths.save_version = 0


def material(name, color, metallic=0, roughness=.65, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metallic
    bs.inputs['Roughness'].default_value = roughness
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    return m


ivory = material('Ivory ceramic - broad matte shell', (.74, .70, .58), .10, .51)
ivory_edge = material('Ivory alternate facet', (.53, .50, .43), .08, .58)
dark = material('Graphite flexible joints', (.025, .036, .042), .2, .60)
copper = material('Aged copper halo', (.39, .19, .075), .58, .48)
copper_edge = material('Copper side facets', (.20, .085, .031), .48, .57)
cyan = material('Cyan core and charge cues', (.025, .72, .95), .1, .31, 3)
white = material('Core center', (.38, .94, 1), .0, .26, 4)
cloth = material('Dark hip tabard', (.047, .048, .046), .0, .93)
floor_mat = material('Studio ground', (.15, .18, .20), .0, .9)
label_mat = material('Review labels', (.48, .60, .64), 0, .8, .5)
model_objects = []


def mesh(name, verts, faces, mat, alternate=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.data.materials.append(mat)
    if alternate:
        obj.data.materials.append(alternate)
        for poly in obj.data.polygons:
            if poly.index % 5 == 0:
                poly.material_index = 1
    model_objects.append(obj)
    return obj


def ellipsoid(name, location, scale, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    model_objects.append(obj)
    return obj


def tapered(name, a, b, radii, mat, sides=8, alternate=None):
    a, b = Vector(a), Vector(b)
    axis = b - a
    q = axis.to_track_quat('Z', 'Y')
    verts = []
    for fraction, rx, ry in radii:
        for i in range(sides):
            angle = 2 * math.pi * (i + .5) / sides
            verts.append(a + axis * fraction + q @ Vector((rx*math.cos(angle), ry*math.sin(angle), 0)))
    faces = [tuple(reversed(range(sides)))]
    for ring in range(len(radii)-1):
        for i in range(sides):
            faces.append((ring*sides+i, ring*sides+(i+1)%sides,
                          (ring+1)*sides+(i+1)%sides, (ring+1)*sides+i))
    faces.append(tuple(range((len(radii)-1)*sides, len(radii)*sides)))
    return mesh(name, verts, faces, mat, alternate)


def bar(name, a, b, radius, mat):
    return tapered(name, a, b, [(0, radius, radius), (1, radius, radius)], mat, 6)


def plate(name, outline, front_y, back_y, mat, ridge=.025):
    # Convex faceted shield. Outline uses x,z pairs, allowing custom ceramic panels.
    n = len(outline)
    verts = [(x, front_y, z) for x,z in outline] + [(x, back_y, z) for x,z in outline]
    verts += [(sum(x for x,z in outline)/n, front_y-ridge, sum(z for x,z in outline)/n)]
    faces = [(i, (i+1)%n, 2*n) for i in range(n)]
    faces += [(i, n+i, n+(i+1)%n, (i+1)%n) for i in range(n)]
    faces += [tuple(range(n, 2*n))]
    return mesh(name, verts, faces, mat)


def disc(name, pos, radius, depth, mat):
    return tapered(name, (pos[0], pos[1]-depth/2, pos[2]),
                   (pos[0], pos[1]+depth/2, pos[2]), [(0,radius,radius),(1,radius,radius)], mat, 20)


# Dark inner body: narrow abdomen, long limbs, separately editable joints.
tapered('Torso inner chassis', (0,0,1.04), (0,0,1.66),
        [(0,.13,.08),(.28,.095,.068),(.70,.20,.095),(1,.115,.07)], dark)
tapered('Neck', (0,0,1.62), (0,-.015,1.82), [(0,.065,.05),(1,.05,.046)], dark)
ellipsoid('Pelvis inner chassis', (0,0,1.04), (.16,.092,.115), dark, 2)

# Blank tapered mask, front is negative Y. No eyes or mouth.
tapered('Blank ivory mask', (0,-.046,1.735), (0,-.005,2.015),
        [(0,.015,.028),(.18,.040,.057),(.56,.088,.082),(.84,.080,.075),(1,.040,.045)], ivory, 12)
bar('Mask cyan seam lower', (0,-.078,1.744), (0,-.114,1.86), .006, cyan)
bar('Mask cyan seam upper', (0,-.114,1.86), (0,-.083,1.99), .006, cyan)
bar('Neck cyan conductor', (0,-.062,1.67), (0,-.070,1.755), .008, cyan)

for side, s in [('L',1),('R',-1)]:
    def p(x,y,z):
        return (s*x,y,z)
    # Front cuirass leaves the core and dark waist visible.
    plate(side+' chest ceramic', [(s*x,z) for x,z in [(.025,1.66),(.15,1.66),(.215,1.57),(.17,1.42),(.083,1.39),(.06,1.47),(.095,1.57)]],
          -.105, -.034, ivory, .02)
    plate(side+' shoulder cap', [(s*x,z) for x,z in [(.195,1.68),(.29,1.635),(.365,1.49),(.286,1.46),(.212,1.535)]],
          -.084, .082, ivory, .037)
    shoulder, elbow, wrist = p(.265,0,1.555), p(.373,-.008,1.30), p(.475,-.035,1.045)
    ellipsoid(side+' shoulder dark ball', shoulder, (.078,.071,.079), dark, 2)
    tapered(side+' upper arm', shoulder, elbow, [(0,.052,.051),(.3,.062,.05),(1,.043,.043)], dark)
    ellipsoid(side+' elbow joint', elbow, (.052,.047,.059), dark, 2)
    tapered(side+' forearm inner', elbow, wrist, [(0,.036,.037),(1,.028,.03)], dark)
    tapered(side+' forearm ceramic', p(.38,-.015,1.30), p(.474,-.043,1.065),
            [(0,.065,.055),(.24,.072,.058),(1,.027,.033)], ivory, 7)
    disc(side+' elbow cyan', p(.373,-.054,1.30), .017, .009, cyan)
    ellipsoid(side+' wrist', wrist, (.031,.03,.034), dark)
    palm_center = p(.49,-.041,.996)
    ellipsoid(side+' palm five-digit base', palm_center, (.045,.027,.063), dark, 2)
    for digit, dx, length in [('index',-.027,.098),('middle',-.009,.113),('ring',.009,.103),('little',.027,.083)]:
        x = .49+dx
        a = p(x,-.047,.965)
        b = p(x+dx*.35,-.055,.965-length*.57)
        c = p(x+dx*.37,-.085,.965-length)
        bar(side+' '+digit+' proximal', a,b,.0095,dark)
        bar(side+' '+digit+' distal', b,c,.008,dark)
    bar(side+' thumb proximal',p(.455,-.044,1.01),p(.432,-.068,.97),.013,dark)
    bar(side+' thumb distal',p(.432,-.068,.97),p(.434,-.10,.943),.010,dark)
    disc(side+' wrist cyan',p(.476,-.068,1.045),.010,.008,cyan)
    # Long slender thighs; porcelain armor confined to prominent surfaces.
    hip, knee, ankle = p(.123,0,1.055),p(.185,.003,.60),p(.219,.008,.145)
    ellipsoid(side+' hip joint', hip, (.085,.075,.087), dark, 2)
    tapered(side+' thigh dark frame',hip,knee,[(0,.070,.059),(.3,.069,.058),(1,.041,.044)],dark)
    tapered(side+' thigh ceramic',p(.147,-.025,1.03),p(.183,-.028,.69),
            [(0,.055,.054),(.34,.069,.065),(1,.045,.038)],ivory,7)
    ellipsoid(side+' knee joint',knee,(.052,.052,.059),dark,2)
    disc(side+' knee cyan',p(.185,-.055,.60),.014,.01,cyan)
    tapered(side+' shin inner',knee,ankle,[(0,.037,.04),(1,.027,.030)],dark)
    tapered(side+' shin ceramic',p(.190,-.011,.55),p(.219,-.002,.17),
            [(0,.065,.060),(.27,.070,.064),(.72,.037,.033),(1,.025,.027)],ivory,7)
    ellipsoid(side+' ankle',ankle,(.036,.046,.040),dark,2)
    ellipsoid(side+' foot dark base',p(.225,-.037,.064),(.077,.135,.058),dark,1)
    for toe, dx in [('inner',-.031),('outer',.031)]:
        x=.225+dx
        foot_verts=[p(x+offset,y,z) for offset,y,z in
                    [(-.030,-.163,.016),(.030,-.163,.016),(.035,.052,.016),(-.035,.052,.016),
                     (-.025,-.158,.062),(.025,-.158,.062),(.027,-.015,.123),(-.027,-.015,.123),
                     (-.029,.052,.079),(.029,.052,.079)]]
        mesh(side+' '+toe+' foot ceramic',foot_verts,
             [(0,1,5,4),(4,5,6,7),(7,6,9,8),(0,4,7,8,3),(1,2,9,6,5),(2,3,8,9),(0,3,2,1)],ivory)
    plate(side+' waist ceramic rim',[(s*x,z) for x,z in [(.032,1.10),(.123,1.155),(.164,1.11),(.128,1.075),(.048,1.055)]],-.085,-.024,ivory,.012)
    # Back scapula is quieter than the front and leaves a clear central charge node.
    back = plate(side+' rear scapula ceramic',[(s*x,z) for x,z in [(.055,1.64),(.16,1.65),(.22,1.54),(.15,1.455),(.071,1.48)]],-.035,.035,ivory,.012)
    back.location.y = .12

disc('Sternum dark socket',(0,-.123,1.53),.088,.035,dark)
disc('Sternum cyan ring',(0,-.147,1.53),.062,.018,cyan)
disc('Sternum white center',(0,-.160,1.53),.038,.009,white)
bar('Abdomen cyan accent',(0,-.082,1.32),(0,-.099,1.42),.009,cyan)
disc('Rear cyan charge node',(0,.12,1.515),.043,.025,cyan)
bar('Rear spine accent',(0,.105,1.29),(0,.115,1.47),.009,cyan)

# Knee-length front/rear hip cloth panels; stiff blockout only, no cloth simulation.
for name, y, sign in [('Front',-.108,-1),('Rear',.102,1)]:
    panel = plate(name+' dark tabard - knee length proposal',
                  [(-.078,1.08),(.078,1.08),(.088,.68),(.052,.615),(-.060,.63),(-.086,.685)],
                  y,y+sign*.012,cloth,.003)
    for s in [-1,1]:
        bar(name+' copper cloth edging '+str(s),(s*.078,y+sign*.006,1.06),(s*.075,y+sign*.006,.67),.004,copper)
    bar(name+' tabard vertical motif',(0,y+sign*.009,.96),(0,y+sign*.009,.74),.0035,copper)

# Four equal arc pieces, actual custom mesh instead of a decorative torus.
halo_y = .235
halo_z = 1.735
outer, inner, thick = .445,.338,.045
for segment in range(4):
    lo = math.radians(segment*90+4)
    hi = math.radians((segment+1)*90-4)
    steps = 10
    verts=[]
    for i in range(steps+1):
        angle=lo+(hi-lo)*i/steps
        for r,y in [(inner,halo_y-thick/2),(outer,halo_y-thick/2),(outer,halo_y+thick/2),(inner,halo_y+thick/2)]:
            verts.append((r*math.sin(angle),y,halo_z+r*math.cos(angle)))
    faces=[(3,2,1,0)]
    for i in range(steps):
        for j in range(4):
            faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
    faces.append(tuple(range(steps*4,(steps+1)*4)))
    mesh('Halo quadrant '+str(segment+1),verts,faces,copper,copper_edge)
for i in range(4):
    angle=math.radians(i*90)
    r=(outer+inner)/2
    center=Vector((r*math.sin(angle),halo_y,halo_z+r*math.cos(angle)))
    tangent=Vector((math.cos(angle),0,-math.sin(angle)))
    bar('Halo charge gap '+str(i+1),center-tangent*.020,center+tangent*.020,.009,cyan)
for side,s in [('L',-1),('R',1)]:
    bar(side+' yoke spine strut',(s*.040,.12,1.48),(s*.11,.20,1.37),.022,dark)
    bar(side+' yoke halo strut',(s*.11,.20,1.37),(s*.20,halo_y,1.405),.022,dark)

model_collection=bpy.data.collections.new('HOLLOW SAINT - editable blockout parts')
scene.collection.children.link(model_collection)
for obj in model_objects:
    for coll in list(obj.users_collection):
        coll.objects.unlink(obj)
    model_collection.objects.link(obj)
    obj['status']='PROPOSED BLOCKOUT - not rigged, not game ready'

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002))
bpy.context.object.name='Studio floor'
bpy.context.object.data.materials.append(floor_mat)


def area(name, loc, energy, size, color):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=energy
    data.shape='DISK'
    data.size=size
    data.color=color
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector((0,0,1.1))-obj.location).to_track_quat('-Z','Y').to_euler()


area('Large warm key',(-3,-4,5),600,4,(1,.89,.74))
area('Soft cool fill',(3,-1,3.5),350,3,(.67,.85,1))
area('Rear silhouette light',(0,3,4),650,3,(.80,.91,1))


def camera(name, loc, target, scale):
    data=bpy.data.cameras.new(name)
    data.type='ORTHO'
    data.ortho_scale=scale
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj


cameras=[
    ('front',camera('Front - orthographic',(0,-7,1.11),(0,0,1.11),2.61)),
    ('side',camera('Side - orthographic',(7,0,1.11),(0,0,1.11),2.61)),
    ('back',camera('Back - orthographic',(0,7,1.11),(0,0,1.11),2.61)),
    ('gameplay-distance',camera('Gameplay distance - simulated camera',(-3.9,7.0,4.4),(0,0,1.03),10.0)),
]
for key,cam in cameras:
    cam['view_note']='SIMULATED CAMERA STUDY - not an in-game screenshot' if key=='gameplay-distance' else 'Actual Blender blockout render'

# Keep a useful editable viewport and the front camera active in the saved file.
scene.camera=cameras[0][1]
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active=model_objects[0]
model_objects[0].select_set(True)
for screen in bpy.data.screens:
    for space in screen.areas:
        if space.type=='VIEW_3D':
            space.spaces.active.region_3d.view_distance=3.5
            space.spaces.active.region_3d.view_location=(0,0,1.1)
scene['review_status']='Milestone 2 custom blockout. Requires user review before detailed modeling or rigging.'
scene['reference_priority']='Selected LEFT A + detail sheet; full four-quarter halo and knee-length tabard are reconciled proposals.'
blend_path=OUT/'hollow-saint-blockout-v1.blend'


def render_label(cam, text, location, size):
    data=bpy.data.curves.new(text,'FONT')
    data.body=text
    data.size=size
    data.space_character=1.15
    obj=bpy.data.objects.new(text,data)
    scene.collection.objects.link(obj)
    obj.parent=cam
    obj.location=location
    obj.data.materials.append(label_mat)
    return obj

stats={
    'status':'PROPOSED BLOCKOUT; not rigged, animated, exported, or tested in game',
    'body_height_m':2.015,
    'halo_outer_diameter_m':outer*2,
    'halo_thickness_m':thick,
    'halo_quadrants':4,
    'model_mesh_objects':len(model_objects),
    'model_vertices':sum(len(o.data.vertices) for o in model_objects),
    'model_polygons':sum(len(o.data.polygons) for o in model_objects),
    'model_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in model_objects),
    'front_tabard_bottom_m':.615,
    'knee_center_m':.60,
    'fingers_per_hand':5,
    'renders':[],
    'render_warning':'Gameplay distance is a simulated Blender view, not an in-game screenshot.'
}
labels={}
for key,cam in cameras:
    scene.camera=cam
    if key=='gameplay-distance':
        scene.render.resolution_x=1600
        scene.render.resolution_y=1000
    else:
        scene.render.resolution_x=1000
        scene.render.resolution_y=1200
    frame=cam.data.view_frame(scene=scene)
    left=min(v.x for v in frame)
    top=max(v.y for v in frame)
    bottom=min(v.y for v in frame)
    margin=(top-bottom)*.028
    size=(top-bottom)*.020
    labels[key]=[
        render_label(cam,'HOLLOW SAINT / BLOCKOUT 01',(left+margin,top-margin-size,-1),size),
        render_label(cam,key.upper()+' / '+('SIMULATED BLENDER VIEW' if key=='gameplay-distance' else 'ORTHOGRAPHIC'),
                     (left+margin,top-margin-size*2.4,-1),size*.65),
        render_label(cam,'NOT IN GAME / NO RIG / PROPOSED CONSTRUCTION' if key=='gameplay-distance' else 'PROPOSED CONSTRUCTION / NO RIG / BODY 2.02 M',
                     (left+margin,bottom+margin,-1),size*.58)
    ]
    for label_key,objects in labels.items():
        for obj in objects:
            obj.hide_render=(label_key!=key)
    scene.render.filepath=str(OUT/('hollow-saint-blockout-v1-'+key+'.png'))
    bpy.ops.render.render(write_still=True)
    stats['renders'].append(scene.render.filepath)
    print('RENDER COMPLETE:',key,flush=True)
scene.camera=cameras[0][1]
scene.render.resolution_x=1000
scene.render.resolution_y=1200
for label_key,objects in labels.items():
    for obj in objects:
        obj.hide_render=(label_key!='front')
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
(OUT/'hollow-saint-blockout-v1-metrics.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats,indent=2),flush=True)
