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
