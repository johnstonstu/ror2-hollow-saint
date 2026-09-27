"""Keep corrected rear graphite integrated with retained UV color at its seams."""
def blend_posterior(body):
    source=body.data.materials[0]
    source_shader=next(n for n in source.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    image_node=next(n for n in source.node_tree.nodes if n.type=='TEX_IMAGE')
    material=body.data.materials[1].copy()
    material.name='HYBRID posterior graphite | retained texture seam blend'
    body.data.materials[1]=material
    tree=material.node_tree
    shader=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
    old=tree.nodes.new('ShaderNodeTexImage')
    old.image=image_node.image
    old.interpolation=image_node.interpolation
    geometry=tree.nodes.new('ShaderNodeNewGeometry')
    separate=tree.nodes.new('ShaderNodeSeparateXYZ')
    tree.links.new(geometry.outputs['Position'],separate.inputs[0])
    def mapped(socket,lo,hi):
        node=tree.nodes.new('ShaderNodeMapRange')
        node.clamp=True
        node.interpolation_type='SMOOTHERSTEP'
        node.inputs['From Min'].default_value=lo
        node.inputs['From Max'].default_value=hi
        tree.links.new(socket,node.inputs['Value'])
        return node.outputs['Result']
    zm=mapped(separate.outputs['Z'],1.255,1.315)
    ym=mapped(separate.outputs['Y'],.055,.085)
    product=tree.nodes.new('ShaderNodeMath')
    product.operation='MULTIPLY'
    tree.links.new(zm,product.inputs[0])
    tree.links.new(ym,product.inputs[1])
    mix=tree.nodes.new('ShaderNodeMixRGB')
    tree.links.new(product.outputs[0],mix.inputs[0])
    tree.links.new(old.outputs['Color'],mix.inputs[1])
    mix.inputs[2].default_value=shader.inputs['Base Color'].default_value[:]
    tree.links.new(mix.outputs['Color'],shader.inputs['Base Color'])
    material['scope']='Center back graphite; narrow Z/Y transition retains source UV image. No emission.'


def clean_shoulder_seams(body,cx):
    """Hide only obsolete posterior shoulder texture in a bounded joint band."""
    for material in [body.data.materials[0],body.data.materials[1]]:
        tree=material.node_tree
        shader=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
        base=shader.inputs['Base Color'].links[0].from_socket
        geometry=tree.nodes.new('ShaderNodeNewGeometry')
        separate=tree.nodes.new('ShaderNodeSeparateXYZ')
        tree.links.new(geometry.outputs['Position'],separate.inputs[0])
        def math_node(op,a,b=None):
            node=tree.nodes.new('ShaderNodeMath')
            node.operation=op
            for index,value in enumerate([a,b]):
                if value is None:continue
                if isinstance(value,(float,int)):node.inputs[index].default_value=value
                else:tree.links.new(value,node.inputs[index])
            return node.outputs[0]
        def ramp(socket,lo,hi):
            node=tree.nodes.new('ShaderNodeMapRange')
            node.clamp=True
            node.interpolation_type='SMOOTHERSTEP'
            node.inputs['From Min'].default_value=lo
            node.inputs['From Max'].default_value=hi
            tree.links.new(socket,node.inputs['Value'])
            return node.outputs['Result']
        distance=math_node('ABSOLUTE',math_node('SUBTRACT',separate.outputs['X'],cx))
        masks=[ramp(distance,.13,.155),ramp(distance,.235,.205),
               ramp(separate.outputs['Z'],1.44,1.475),ramp(separate.outputs['Z'],1.75,1.715),
               ramp(separate.outputs['Y'],.02,.05)]
        factor=masks[0]
        for mask in masks[1:]:factor=math_node('MULTIPLY',factor,mask)
        mix=tree.nodes.new('ShaderNodeMixRGB')
        mix.label='Bounded clean posterior shoulder joint'
        tree.links.new(factor,mix.inputs[0])
        tree.links.new(base,mix.inputs[1])
        mix.inputs[2].default_value=(.014,.020,.024,1)
        tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
