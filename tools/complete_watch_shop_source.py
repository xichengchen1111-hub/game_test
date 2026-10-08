"""Add editable reference architecture to the shop .blend, using Unity's exported layout.

Unity remains the authority for colliders, shell and moving doors. This collection is
excluded from the visual-kit FBX, so there are no duplicate walls in the game.
Run after a successful Unity generation to refresh complete Blender review views.
"""
import argparse
import json
import math
import sys
from pathlib import Path
import bpy
from mathutils import Vector


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',required=True,type=Path)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    root=args.root.resolve()
    source=root/'assets/source/watch-shop'
    bpy.ops.wm.open_mainfile(filepath=str(source/'WalnutShop.blend'))
    bpy.context.preferences.filepaths.save_version=0
    layout=json.loads((source/'layout.json').read_text(encoding='utf-8-sig'))
    for collection_name in ('ArchitectureReference','FinalReview'):
        previous=bpy.data.collections.get(collection_name)
        if previous:
            for obj in list(previous.objects):
                bpy.data.objects.remove(obj,do_unlink=True)
            bpy.data.collections.remove(previous)
    col=bpy.data.collections.new('ArchitectureReference');bpy.context.scene.collection.children.link(col)
    def u(p):return -p[0],-p[2],p[1]
    ivory=bpy.data.materials.get('ShopIvory')
    green=bpy.data.materials.get('ShopGreen')
    glass=bpy.data.materials.get('ShopGlass')
    def box(name,pos,size,mat=ivory):
        bpy.ops.mesh.primitive_cube_add(size=1,location=u(pos));o=bpy.context.object;o.name=name
        o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        for c in list(o.users_collection):c.objects.unlink(o)
        col.objects.link(o)
        if mat:o.data.materials.append(mat)
        return o
    w,d=layout['footprint']['x'],layout['footprint']['y']
    h,t=layout['wallHeight'],layout['wallThickness']
    for wall in range(4):
        span=w if wall<2 else d-2*t
        centre=(0,0,-(d-t)/2) if wall==0 else (0,0,(d-t)/2) if wall==1 else (-(w-t)/2,0,0) if wall==2 else ((w-t)/2,0,0)
        def piece(a,b,y0,y1):
            if b-a<.001 or y1-y0<.001:return
            x=(a+b)/2;cy=(y0+y1)/2
            # WallRotation as in BuildingLayout: Front=0, Back=180, Left=90, Right=-90.
            offset=x if wall==0 else -x if wall==1 else 0
            zoffset=-x if wall==2 else x if wall==3 else 0
            size=(b-a,y1-y0,t) if wall<2 else (t,y1-y0,b-a)
            box('Reference wall', (centre[0]+offset,cy,centre[2]+zoffset),size)
        cursor=-span/2
        for opening in sorted((o for o in layout['openings'] if o['wall']==wall),key=lambda o:o['offset']):
            ow,oh=opening['size']['x'],opening['size']['y'];x=opening['offset'];y=opening['centreHeight']
            piece(cursor,x-ow/2,0,h);piece(x-ow/2,x+ow/2,0,y-oh/2);piece(x-ow/2,x+ow/2,y+oh/2,h)
            if opening['kind']==1:
                # Open leaf for source preview, inset lower wood panel and clear upper glazing.
                front=wall==0;z=centre[2];sign=1 if front else -1;hx=-ow/2 if front else ow/2
                for end in (0,ow-.10):box('Reference door stile',(hx,oh/2,z+sign*end),(.10,oh-.04,.10),green)
                for y in (.06,.89,oh-.06):box('Reference door rail',(hx,y,z+sign*(ow-.1)/2),(.10,.12,ow-.20),green)
                box('Reference door lower panel',(hx,.45,z+sign*(ow-.1)/2),(.06,.70,ow-.24),green)
                leaf=box('Reference door transparent pane',(hx,(oh+.91)/2,z+sign*(ow-.1)/2),(.006,oh-1.02,ow-.24),glass)
                leaf['runtime_source']='BuildSlice.Walnut.ShopDoor + ShopMechanism'
            else:
                px=x if wall==0 else -x
                box('Reference transparent pane',(px,y,centre[2]),(ow,oh,.006),glass)
            cursor=x+ow/2
        piece(cursor,span/2,0,h)
    ceiling=box('Reference ceiling',(0,3.37,0),(w,.14,d));roof=box('Reference low roof',(0,3.52,0),(w+.25,.16,d+.25),bpy.data.materials.get('ShopIron'))
    floor=box('Reference subfloor',(0,-.10,0),(w,.18,d),bpy.data.materials.get('ShopOak'))
    def dial(pos,diameter,yaw=0):
        a=math.radians(yaw)
        def point(x,y,z=0):return u((pos[0]+x*math.cos(a)+z*math.sin(a),pos[1]+y,pos[2]-x*math.sin(a)+z*math.cos(a)))
        vertices=[point(0,0)]+[point(math.sin(i*math.tau/64)*diameter/2,math.cos(i*math.tau/64)*diameter/2) for i in range(64)]
        mesh=bpy.data.meshes.new('Circular reference dial');mesh.from_pydata(vertices,[],[(0,1+i,1+(i+1)%64) for i in range(64)]);mesh.update()
        obj=bpy.data.objects.new('Reference circular clock face',mesh);col.objects.link(obj);mesh.materials.append(bpy.data.materials['ShopPaper'])
        # These are reference-only copies of runtime HeritageDial, never an opaque square decal.
        for i in range(12):
            angle=i*math.tau/12;x=math.sin(angle)*diameter*.42;y=math.cos(angle)*diameter*.42
            centre=(pos[0]+x*math.cos(a),pos[1]+y,pos[2]-x*math.sin(a)-.015)
            tick=box('Reference hour index',centre,(diameter*.018,diameter*.060,.009),bpy.data.materials['ShopIron'])
            tick.rotation_euler.y=-angle;tick.rotation_euler.z=-a
        for angle,length,width in [(-.5,.36,.022),(-2.8,.25,.035)]:
            x=math.sin(angle)*diameter*length*.4;y=math.cos(angle)*diameter*length*.4
            hand=box('Reference clock hand',(pos[0]+x*math.cos(a),pos[1]+y,pos[2]-x*math.sin(a)-.025),(diameter*width,diameter*length,.01),bpy.data.materials['ShopIron'])
            hand.rotation_euler.y=-angle;hand.rotation_euler.z=-a
    dial((0,3.68,-4.32),.67)
    for side in (-1,1):dial((-3.65+side*.10,2.59,-4.75),.50,90 if side<0 else -90)
    for name,file in [('ShopWalnut','Wood.png'),('ShopGreen','Wood.png'),('ShopOak','Wood.png'),('ShopIvory','Plaster.png'),('ShopRug','Rug.png')]:
        material=bpy.data.materials.get(name)
        if not material:continue
        material.use_nodes=True;nodes=material.node_tree.nodes;shader=nodes.get('Principled BSDF')
        tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'game/Assets/Art/WalnutShop'/file),check_existing=True);tex.image.pack()
        multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
        multiply.inputs[1].default_value=shader.inputs['Base Color'].default_value
        material.node_tree.links.new(tex.outputs['Color'],multiply.inputs[2]);material.node_tree.links.new(multiply.outputs[0],shader.inputs['Base Color'])
    scene=bpy.context.scene
    review=bpy.data.collections.new('FinalReview');scene.collection.children.link(review)
    for light in [o for o in scene.objects if o.type=='LIGHT']:light.hide_render=True
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.19,.28,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
    def lamp(name,pos,power,color,size):
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
        obj=bpy.data.objects.new(name,data);review.objects.link(obj);obj.location=u(pos)
        obj.rotation_euler=(Vector(u((pos[0],0,pos[2])))-obj.location).to_track_quat('-Z','Y').to_euler()
    lamp('Window dusk',(0,5,-6),450,(.58,.70,1),6)
    lamp('Interior warm',(-1.4,3.05,-.5),220,(1,.75,.47),2)
    lamp('Rear warm',(1.6,3,2.4),180,(1,.78,.52),2)
    camera=bpy.data.objects.new('Final review camera',bpy.data.cameras.new('Final review camera'));review.objects.link(camera);scene.camera=camera
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    output=root/'output/demo/walnut-shop';output.mkdir(parents=True,exist_ok=True)
    def render(name,pos,target,lens):
        camera.location=u(pos);camera.rotation_euler=(Vector(u(target))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=lens
        scene.render.filepath=str(output/name);bpy.ops.render.render(write_still=True)
    render('model-complete-exterior.png',(-8,3.4,-12),(0,1.8,-1.5),38)
    render('model-complete-interior.png',(0,1.66,-3.5),(0,1.5,2.7),22)
    bpy.ops.wm.save_as_mainfile(filepath=str(source/'WalnutShop.blend'),compress=True)
    bpy.ops.wm.open_mainfile(filepath=str(source/'WalnutShop.blend'))
    report={'sourceReopened':True,'shellFrom':'layout.json exported from actual Unity BuildingLayout','referenceArchitectureExcludedFromFbx':True,
        'parts':len(bpy.data.collections['ArchitectureReference'].objects),'blender':bpy.app.version_string}
    (source/'complete-source-results.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('WATCH_SHOP_COMPLETE_SOURCE_PASS',json.dumps(report))


if __name__=='__main__':main()
