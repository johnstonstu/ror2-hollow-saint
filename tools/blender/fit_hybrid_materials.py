"""Retain source UV texture while adding bounded cyan light and useful surfaces."""
import bpy


def tune_body_material(body):
    import math
    source = body.data.materials[0]
    material = source.copy()
    material.name = 'HYBRID source texture | ceramic graphite and cyan'
    body.data.materials[0] = material
    tree = material.node_tree
    shader = next(node for node in tree.nodes if node.type == 'BSDF_PRINCIPLED')
    color = shader.inputs['Base Color'].links[0].from_socket
    shader.inputs['Metallic'].default_value = .06
    shader.inputs['Roughness'].default_value = .57
    separate = tree.nodes.new('ShaderNodeSeparateColor')
    separate.mode = 'RGB'
    separate.label = 'Source cyan: both green and blue exceed red'
    tree.links.new(color, separate.inputs['Color'])

    def math_node(operation, a, b):
        node = tree.nodes.new('ShaderNodeMath')
        node.operation = operation
        for index, value in enumerate([a,b]):
            if isinstance(value, (int,float)):
                node.inputs[index].default_value = value
            else:
                tree.links.new(value, node.inputs[index])
        return node.outputs[0]

    luminance = tree.nodes.new('ShaderNodeRGBToBW')
    tree.links.new(color, luminance.inputs['Color'])
    brightness = math_node('MULTIPLY', luminance.outputs[0], 2)
    metal = math_node('SUBTRACT', .65, brightness)
    metal = math_node('MAXIMUM', metal, .06)
    tree.links.new(metal, shader.inputs['Metallic'])

    green = math_node('SUBTRACT', separate.outputs['Green'], separate.outputs['Red'])
    blue = math_node('SUBTRACT', separate.outputs['Blue'], separate.outputs['Red'])
    saturation = math_node('MINIMUM', green, blue)
    mask = math_node('SUBTRACT', saturation, .055)
    mask = math_node('MULTIPLY', mask, 12)
    mask = math_node('MAXIMUM', mask, 0)
    mask = math_node('MINIMUM', mask, 1)
    strength = math_node('MULTIPLY', mask, .85)
    tree.links.new(color, shader.inputs['Emission Color'])
    tree.links.new(strength, shader.inputs['Emission Strength'])
    material['cyan_emission'] = 'Bounded 0..0.85; only source texels with G and B > R+.055'
    material['raster_changes'] = 'None; source packed image unchanged'
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    body.data.set_sharp_from_angle(angle=math.radians(45))
    body['surface_normals'] = 'Smooth below 45 degrees; no vertex positions changed'
    return material


def tune_copper(material):
    tree = material.node_tree
    shader = next(node for node in tree.nodes if node.type == 'BSDF_PRINCIPLED')
    texcoord = tree.nodes.new('ShaderNodeTexCoord')
    noise = tree.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 8
    noise.inputs['Detail'].default_value = 2
    noise.inputs['Roughness'].default_value = .6
    tree.links.new(texcoord.outputs['Object'], noise.inputs['Vector'])
    ramp = tree.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = .22
    ramp.color_ramp.elements[0].color = (.26,.085,.027,1)
    ramp.color_ramp.elements[1].position = .78
    ramp.color_ramp.elements[1].color = (.46,.215,.084,1)
    tree.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    tree.links.new(ramp.outputs['Color'], shader.inputs['Base Color'])
    shader.inputs['Metallic'].default_value = .7
    shader.inputs['Roughness'].default_value = .48
    material['finish'] = 'Broad restrained procedural aged copper variation'


if __name__ == '__main__':
    from pathlib import Path
    if not bpy.app.background:
        raise RuntimeError('Material trial must use isolated background Blender')
    root = Path(__file__).resolve().parents[2]
    out = root/'art'/'material-studies'
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(root/'art'/'hybrid'/'hollow-saint-hybrid-v3.blend'))
    body = next(o for o in bpy.context.scene.objects if o.name.startswith('HF BODY |'))
    tune_body_material(body)
    tune_copper(bpy.data.materials['HYBRID aged copper'])
    scene = bpy.context.scene
    scene.camera = bpy.data.objects['Hero three quarter']
    scene.render.resolution_x,scene.render.resolution_y = 1100,1400
    scene.render.filepath = str(out/'hybrid-v3-material-trial2-hero.png')
    bpy.ops.render.render(write_still=True)
    print('Material trial render complete; no source scene saved or raster edited',flush=True)
