"""Editable original Walnut Heritage watch-shop joinery, displays and precision props.

blender --background --python-exit-code 1 --python tools/build_walnut_shop.py -- --root REPO
"""
import argparse
import json
import math
import random
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector, Matrix


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    root=args.root.resolve();source=root/'assets/source/watch-shop';art=root/'game/Assets/Art/WalnutShop'
    source.mkdir(parents=True,exist_ok=True);art.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
    scene=bpy.context.scene;scene.unit_settings.system='METRIC'
    collection=bpy.data.collections.new('ShopVisual');scene.collection.children.link(collection)
    # FBX -> Unity reflects the X axis. Author that reflection explicitly so the
    # imported EastMarker and rear-right workstation agree with the common layout.
    def u(p):return Vector((-p[0],-p[2],p[1]))
    colors={'ShopWalnut':'69492F','ShopGreen':'365246','ShopBrass':'BA9958','ShopCopper':'857062','ShopIvory':'D1C2A7',
        'ShopFelt':'36553E','ShopGlass':'B4D2D7','ShopGlow':'FFD18A','ShopPaper':'E6D6B5','ShopOak':'B88A58','ShopIron':'303739','ShopRug':'795444'}
    mats={}
    for name,color in colors.items():
        values=[int(color[i:i+2],16)/255 for i in (0,2,4)];linear=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in values]
        m=bpy.data.materials.new(name);m.diffuse_color=(*linear,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
        p.inputs['Base Color'].default_value=m.diffuse_color;p.inputs['Roughness'].default_value=.62
        if name in ('ShopBrass','ShopCopper'):p.inputs['Metallic'].default_value=.65;p.inputs['Roughness'].default_value=.32
        if name=='ShopGlass':p.inputs['Transmission Weight'].default_value=1;p.inputs['Roughness'].default_value=.05;p.inputs['IOR'].default_value=1.45
        if name=='ShopGlow':p.inputs['Emission Color'].default_value=(*linear,1);p.inputs['Emission Strength'].default_value=1.2
        mats[name]=m
    meshes=[]
    def move_to_collection(obj):
        for c in list(obj.users_collection):c.objects.unlink(obj)
        collection.objects.link(obj)
    def empty(name,pos=(0,0,0)):
        obj=bpy.data.objects.new(name,None);collection.objects.link(obj);obj.matrix_world=Matrix.Translation(u(pos));bpy.context.view_layer.update();return obj
    active_module=empty('Module_Facade')
    def finish(obj,name,material,bevel=0,parent=None):
        obj.name=name;move_to_collection(obj)
        if bevel:
            bm=bmesh.new();bm.from_mesh(obj.data)
            bmesh.ops.bevel(bm,geom=list(bm.edges),offset=bevel,segments=2,affect='EDGES',clamp_overlap=True)
            bm.to_mesh(obj.data);bm.free()
        obj.data.materials.append(mats[material]);meshes.append(obj)
        # Explicit box-projected UVs survive material joins and FBX. Without these
        # a procedural mesh samples only one texel of the grain / woven carpet.
        uv=obj.data.uv_layers.new(name='SurfaceUV')
        coords=[v.co for v in obj.data.vertices]
        lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)))
        hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
        for face in obj.data.polygons:
            axis=max(range(3),key=lambda i:abs(face.normal[i]))
            axes=[i for i in range(3) if i!=axis]
            for loop in face.loop_indices:
                point=coords[obj.data.loops[loop].vertex_index]
                if material=='ShopRug':
                    uv.data[loop].uv=tuple((point[i]-lo[i])/max(.001,hi[i]-lo[i]) for i in axes)
                else:uv.data[loop].uv=tuple(point[i]*1.4 for i in axes)
        world=obj.matrix_world.copy();obj.parent=parent or active_module;obj.matrix_world=world
        return obj
    def box(name,pos,size,mat='ShopWalnut',bevel=.008,parent=None):
        bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
        for vertex in bm.verts:vertex.co.x*=size[0];vertex.co.y*=size[2];vertex.co.z*=size[1]
        mesh=bpy.data.meshes.new(name);bm.to_mesh(mesh);bm.free()
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);obj.matrix_world=Matrix.Translation(u(pos))
        return finish(obj,name,mat,bevel,parent)
    def cylinder(name,pos,radius,depth,mat='ShopBrass',axis=(0,1,0),parent=None,vertices=24):
        bm=bmesh.new();bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=vertices,radius1=radius,radius2=radius,depth=depth)
        mesh=bpy.data.meshes.new(name);bm.to_mesh(mesh);bm.free()
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj)
        obj.matrix_world=Matrix.Translation(u(pos))@u(axis).to_track_quat('Z','Y').to_matrix().to_4x4()
        return finish(obj,name,mat,.002,parent)
    def beam(name,a,b,radius=.012,mat='ShopBrass',parent=None):
        a,b=Vector(a),Vector(b);return cylinder(name,(a+b)/2,radius,(b-a).length,mat,(b-a).normalized(),parent,12)
    def ring(name,pos,radius,thickness=.012,mat='ShopBrass',axis=(0,0,-1),parent=None):
        vertices=[];faces=[]
        for i in range(32):
            a=i*math.tau/32
            for j in range(8):
                b=j*math.tau/8;r=radius+thickness*math.cos(b);vertices.append((r*math.cos(a),r*math.sin(a),thickness*math.sin(b)))
        for i in range(32):
            for j in range(8):faces.append((i*8+j,((i+1)%32)*8+j,((i+1)%32)*8+(j+1)%8,i*8+(j+1)%8))
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
        obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj)
        obj.matrix_world=Matrix.Translation(u(pos))@u(axis).to_track_quat('Z','Y').to_matrix().to_4x4()
        return finish(obj,name,mat,0,parent)
    def framed_panel(name,pos,width,height,mat='ShopGreen',parent=None):
        start=len(meshes)
        x,y,z=pos
        box(name+' field',pos,(width,height,.075),mat,.012,parent)
        for side in (-1,1):
            box(name+' stile',(x+side*(width/2-.045),y,z-.05),(.07,height,.035),mat,.004,parent)
            box(name+' rail',(x,y+side*(height/2-.045),z-.05),(width-.12,.07,.035),mat,.004,parent)
        box(name+' raised centre',(x,y,z-.055),(max(.05,width-.22),max(.04,height-.20),.027),mat,.008,parent)
        return meshes[start:]
    def turn(parts,centre,yaw):
        bpy.context.view_layer.update()
        origin=u(centre);rotation=Matrix.Rotation(-math.radians(yaw),4,'Z')
        for obj in parts:obj.matrix_world=Matrix.Translation(origin)@rotation@Matrix.Translation(-origin)@obj.matrix_world
        bpy.context.view_layer.update()
    # Layered architectural frontage, every member respects the true apertures.
    for x in (-3.46,-.80,.80,3.46):
        box('Green pilaster',(x,1.58,-4.12),(.17,3.16,.20),'ShopGreen',.012)
        box('Pilaster foot',(x,.13,-4.16),(.25,.26,.27),'ShopWalnut',.012)
        box('Pilaster capital',(x,3.10,-4.16),(.29,.16,.28),'ShopWalnut',.008)
        box('Pilaster brass bead',(x-.045,1.7,-4.23),(.012,2.45,.012),'ShopBrass',.002)
    for x in (-2,2):framed_panel('Storefront kick panel',(x,.36,-4.10),2.34,.68)
    for y,width,thickness,depth,mat in [(3.19,7.46,.10,.28,'ShopWalnut'),(3.30,7.62,.10,.34,'ShopGreen'),(3.40,7.52,.065,.30,'ShopBrass'),(4.03,7.5,.13,.34,'ShopWalnut')]:
        box('Layered cornice',(0,y,-4.12),(width,thickness,depth),mat,.009)
    for x in (-2,2):framed_panel('Fascia sign',(x,3.69,-4.10),2.95,.58)
    box('Clock medallion field',(0,3.7,-4.10),(1.25,.68,.17),'ShopGreen',.015)
    cylinder('Facade medallion',(0,3.68,-4.22),.43,.08,'ShopWalnut',(0,0,-1))
    ring('Facade brass rim',(0,3.68,-4.275),.36,.016)
    # Copper canopy with folded seams; actual supporting brackets, no loose posts.
    for i in range(13):
        sheet=box('Canopy copper sheet',(-3.48+i*.58,3.12,-4.50),(.60,.045,.91),'ShopCopper',.006)
        sheet.rotation_euler.x=math.radians(9)
        beam('Canopy standing seam',(-3.76+i*.58,3.20,-4.06),(-3.76+i*.58,3.06,-4.95),.010,'ShopCopper')
    box('Canopy folded front edge',(0,3.045,-4.95),(7.55,.12,.04),'ShopCopper',.005)
    for x in (-3.2,0,3.2):
        beam('Canopy wall bracket',(x,2.75,-4.12),(x,3.10,-4.84),.025,'ShopIron')
    for x in (-2.7,2.7):
        beam('Sconce arm',(x,3.88,-4.16),(x,3.88,-4.5),.025,'ShopIron')
        cylinder('Sconce shade',(x,3.81,-4.5),.19,.07,'ShopCopper')
        cylinder('Sconce bulb',(x,3.75,-4.5),.065,.10,'ShopGlow')
    beam('Side sign bracket',(-3.65,3.0,-4.12),(-3.65,3.0,-4.98),.035,'ShopIron')
    cylinder('Hanging clock case',(-3.65,2.59,-4.75),.32,.14,'ShopWalnut',(1,0,0))
    cylinder('Hanging ivory dial',(-3.74,2.59,-4.75),.27,.012,'ShopIvory',(1,0,0))
    # Individual staggered honey oak planks. Join per material later, preserving topology.
    active_module=empty('Module_OakFloor')
    for i in range(24):
        x=-3.46+i*.3
        boundaries=[-4.0]+[v for v in [-4.0+((i%3)+1)*.68+j*2.04 for j in range(5)] if -3.99<v<3.99]+[4.0]
        for a,b in zip(boundaries,boundaries[1:]):box('Staggered oak plank',(x,.014,(a+b)/2),(.296,.025,b-a-.004),'ShopOak',.002)
    # Wall paneling and interior crown.
    active_module=empty('Module_InteriorJoinery')
    for side in (-1,1):
        for j in range(8):
            pos=(side*3.48,.52,-3.45+j*.94)
            panel=framed_panel('Interior walnut panel',pos,.88,.94,'ShopWalnut')
            turn(panel,pos,90*side)
        for y in (.08,1.03,3.23):box('Wall molding',(side*3.46,y,0),(.10,.07,7.9),'ShopWalnut',.006)
    # Clocks are actual shaped objects with circular bezels, ticks and independent hands.
    def clock(name,pos,size=.35,front=-1,pendulum=False,yaw=0,variant=0):
        start=len(meshes)
        x,y,z=pos
        box(name+' plinth',(x,y+.035,z),(size*1.2,.07,.22),'ShopWalnut',.010)
        if variant%3==1 and not pendulum:
            cylinder(name+' round case',(x,y+size*.58,z),size*.53,.19,'ShopBrass',(0,0,1))
            for side in (-1,1):box('Clock brass foot',(x+side*size*.33,y+.08,z),(.05,.10,.15),'ShopBrass',.008)
        else:
            box(name+' case',(x,y+size*.56,z),(size,size*1.05,.18),'ShopGreen' if variant%3==2 else 'ShopWalnut',.015)
            if variant%3==2 and not pendulum:
                box(name+' stepped cap',(x,y+size*1.14,z),(size*1.12,.055,.23),'ShopWalnut',.008)
                for side in (-1,1):box('Clock brass column',(x+side*size*.43,y+size*.60,z-.10),(.018,size*.73,.018),'ShopBrass',.003)
            else:cylinder(name+' arch',(x,y+size*.86,z),size*.50,.19,'ShopWalnut',(0,0,1))
        cy=y+size*.62;fz=z+front*.105
        cylinder(name+' brass bezel',(x,cy,fz),size*.40,.016,'ShopBrass',(0,0,1))
        cylinder(name+' ivory dial',(x,cy,fz+front*.012),size*.355,.009,'ShopIvory',(0,0,1))
        for n in range(12):
            a=n*math.tau/12;mark=box('Dial tick',(x+math.sin(a)*size*.30,cy+math.cos(a)*size*.30,fz+front*.02),(.008,.027,.008),'ShopIron',0)
            mark.rotation_euler.y=-a
        beam('Decorative minute hand',(x,cy,fz+front*.03),(x+size*.16,cy+size*.19,fz+front*.03),.006,'ShopIron')
        beam('Decorative hour hand',(x,cy,fz+front*.035),(x-size*.11,cy+size*.07,fz+front*.035),.008,'ShopIron')
        if pendulum:
            box('Pendulum cabinet',(x,y-size*.6,z),(size*.7,size*1.35,.17),'ShopWalnut',.012)
            box('Pendulum dark inset',(x,y-size*.56,z+front*.095),(size*.44,size*.98,.012),'ShopIron',.003)
            beam('Pendulum rod',(x,y-size*.1,z+front*.12),(x,y-size*.93,z+front*.12),.012,'ShopBrass')
            cylinder('Pendulum bob',(x,y-size*.93,z+front*.13),size*.16,.02,'ShopBrass',(0,0,1))
        if yaw:turn(meshes[start:],pos,yaw)
    # Low U-shaped perimeter cabinets with glass access leaves.
    for side in (-1,1):
        active_module=empty('Module_'+('Left' if side<0 else 'Right')+'Cabinet')
        x=side*2.85
        end=2.2 if side<0 else 1.10;length=end+2.7;middle=(end-2.7)/2
        box('Cabinet carcass',(x,.55,middle),(.76,1.04,length),'ShopWalnut',.012)
        box('Countertop',(x,1.10,middle),(.88,.09,length+.12),'ShopWalnut',.012)
        count=5 if side<0 else 4
        for j in range(count):
            z=-2.7+(j+.5)*length/count
            panel=box('Inset cabinet door',(x-side*.397,.57,z),(.026,.76,.87),'ShopWalnut',.008)
            box('Panel raised field',(x-side*.417,.57,z),(.018,.59,.68),'ShopWalnut',.008)
            cylinder('Cabinet pull',(x-side*.445,.88,z+.27),.027,.045,'ShopBrass',(1,0,0))
        for z in (-2.7,end):
            for y in (1.17,1.63):box('Vitrine end rail',(x,y,z),(.84,.045,.045),'ShopWalnut',.004)
            for vx in (x-.4,x+.4):box('Vitrine end stile',(vx,1.40,z),(.045,.50,.045),'ShopWalnut',.004)
            box('Vitrine end glass',(x,1.4,z),(.76,.42,.007),'ShopGlass',0)
        for vx in (x-.4,x+.4):
            for y in (1.17,1.63):box('Vitrine long rail',(vx,y,middle),(.045,.045,length),'ShopWalnut',.004)
        box('Vitrine top glass',(x,1.63,middle),(.78,.008,length-.05),'ShopGlass',0)
        # Outer side is fixed, inward-facing access is one independent leaf.
        box('Vitrine outer glass',(x+side*.4,1.4,middle),(.006,.43,length-.07),'ShopGlass',0)
        pivot=empty('VitrineLeftHinge' if side<0 else 'VitrineRightHinge',(x-side*.4,1.17,-2.7))
        for y in (1.185,1.615):box('Access door long rail',(x-side*.4,y,-1.91),(.04,.03,1.52),'ShopWalnut',.003,pivot)
        for z in (-2.67,-1.15):box('Access door end stile',(x-side*.4,1.4,z),(.04,.46,.04),'ShopWalnut',.003,pivot)
        box('Access door clear glass',(x-side*.4,1.4,-1.91),(.006,.41,1.47),'ShopGlass',0,pivot)
        cylinder('Access door handle',(x-side*.44,1.4,-1.35),.025,.04,'ShopBrass',(1,0,0),pivot)
        box('Fixed inner vitrine glass',(x-side*.4,1.4,(end-1.1)/2),(.006,.42,end+1.05),'ShopGlass',0)
        box('Access frame divider',(x-side*.4,1.4,-1.10),(.045,.46,.045),'ShopWalnut',.003)
        clocks=8 if side<0 else 6
        for j in range(clocks):clock('Cabinet mantel clock',(x,1.15,-2.35+j*(length-.70)/(clocks-1)),.23+(j%3)*.035,front=-1,yaw=90*side,variant=j)
        # Two wall shelves and varied clocks read as shaped objects in silhouette.
        for y in (2.0,2.65):
            box('Wall display shelf',(side*3.20,y,.20),(.52,.065,4.4),'ShopWalnut',.007)
            for j in range(6):clock('Shelf clock',(side*3.2,y+.035,-1.6+j*.68),.32+(j%3)*.055,yaw=90*side,variant=j)
    # Window displays face the street.
    for side in (-1,1):
        active_module=empty('Module_WindowDisplay'+str(side))
        box('Window display plinth',(side*2,.38,-3.32),(1.92,.76,.66),'ShopWalnut',.012)
        box('Window velvet tray',(side*2,.777,-3.32),(1.80,.018,.58),'ShopFelt',.004)
        for j in range(4):clock('Window clock',(side*2-.64+j*.42,.79,-3.28),.27+(j%2)*.12,variant=j)
    active_module=empty('Module_RepairBay')
    box('Rear left cabinet',(-2,.64,3.52),(2.18,1.28,.55),'ShopWalnut',.012)
    for x in (-2.7,-2,-1.3):
        for y in (.30,.65,1.0):
            framed_panel('Small drawer face',(x,y,3.222),.64,.29,'ShopWalnut')
            cylinder('Small brass drawer knob',(x,y,3.15),.025,.035,'ShopBrass',(0,0,1))
    clock('Rear pendulum clock',(-2.8,2.28,3.72),.60,pendulum=True)
    clock('Rear pendulum clock',(-1.35,2.35,3.72),.48,pendulum=True)
    # A small period botanical print and flowers soften the repair bay, as in A.
    box('Botanical picture walnut frame',(-2.05,2.38,3.81),(.49,.64,.065),'ShopWalnut',.008)
    box('Botanical picture paper',(-2.05,2.38,3.77),(.40,.55,.008),'ShopPaper',.001)
    beam('Botanical drawn stem',(-2.05,2.17,3.755),(-2.05,2.59,3.755),.006,'ShopGreen')
    for i in range(5):
        side=-1 if i%2 else 1;y=2.23+i*.067
        beam('Botanical leaf',(-2.05,y,3.75),(-2.05+side*.115,y+.05,3.75),.018,'ShopFelt')
    cylinder('Flower vase',(-1.65,1.40,3.48),.085,.23,'ShopGreen',vertices=24)
    ring('Vase brass lip',(-1.65,1.52,3.48),.067,.006,'ShopBrass',(0,1,0))
    for i in range(7):
        a=i*2.4;dx=math.sin(a)*.15;dz=math.cos(a)*.10;top=1.76+(i%3)*.055
        beam('Flower stem',(-1.65,1.48,3.48),(-1.65+dx,top,3.48+dz),.005,'ShopFelt')
        beam('Flower leaf',(-1.65+dx*.5,1.62,3.48+dz*.5),(-1.65+dx*.5+.07,1.68,3.48+dz*.5),.025,'ShopGreen')
        for petal in range(5):
            b=petal*math.tau/5
            cylinder('Ivory flower petal',(-1.65+dx+math.sin(b)*.025,top,3.48+dz+math.cos(b)*.025),.021,.012,'ShopPaper',vertices=12)
        cylinder('Flower golden centre',(-1.65+dx,top+.009,3.48+dz),.012,.01,'ShopBrass',vertices=12)
    box('Repair bench top',(2,.78,1.85),(2.40,.10,.80),'ShopWalnut',.015)
    for x in (.97,3.03):
        if x<2:
            box('Bench pedestal lower carcass',(x,.265,1.85),(.35,.53,.72),'ShopWalnut',.010)
            for side in (-1,1):box('Drawer bay side',(x+side*.163,.635,1.85),(.024,.21,.72),'ShopWalnut',.004)
            box('Drawer bay back',(x,.635,2.198),(.31,.21,.024),'ShopWalnut',.004)
        else:box('Bench pedestal',(x,.37,1.85),(.35,.74,.72),'ShopWalnut',.010)
        for y in (.25,.48,.65):
            if x<2 and y==.65:continue
            framed_panel('Bench drawer face',(x,y,1.475),.31,.18,'ShopWalnut')
            cylinder('Bench pull',(x,y,1.44),.018,.03,'ShopBrass',(0,0,1))
    box('Repair mat',(2.05,.838,1.86),(.82,.014,.52),'ShopFelt',.004)
    for y in (1.62,2.22):
        box('Repair bay shelf',(2.1,y,3.62),(2.65,.07,.54),'ShopWalnut',.008)
        for j in range(7):
            cylinder('Oil bottle',(1.05+j*.34,y+.14,3.59),.045,.20,'ShopGreen',vertices=16)
            cylinder('Bottle brass stopper',(1.05+j*.34,y+.255,3.59),.031,.035,'ShopBrass',vertices=16)
    box('Tool rack',(2.0,1.82,3.32),(1.68,.30,.07),'ShopWalnut',.007)
    for j in range(8):
        x=1.32+j*.19;beam('Hanging tool shank',(x,1.75,3.26),(x,1.35,3.26),.009,'ShopIron')
        cylinder('Tool timber grip',(x,1.66,3.26),.022,.17,'ShopOak')
    # A genuinely partitioned, independent drawer.
    drawer=empty('PartsDrawer',(.97,.55,1.85))
    box('Sliding drawer bottom',(.97,.55,1.80),(.29,.025,.70),'ShopWalnut',.003,drawer)
    for x in (.82,1.12):box('Drawer side',(x,.60,1.80),(.023,.12,.70),'ShopWalnut',.003,drawer)
    for z in (1.45,2.15):box('Drawer end',(.97,.60,z),(.31,.12,.025),'ShopWalnut',.003,drawer)
    box('Drawer velvet',(.97,.568,1.80),(.26,.007,.67),'ShopFelt',.001,drawer)
    for z in (1.72,1.9,2.07):box('Drawer partition',(.97,.605,z),(.27,.075,.012),'ShopOak',.001,drawer)
    box('Drawer cross partition',(.97,.605,1.85),(.012,.075,.58),'ShopOak',.001,drawer)
    box('Parts drawer front',(.97,.61,1.44),(.32,.16,.035),'ShopWalnut',.005,drawer)
    cylinder('Parts drawer knob',(.97,.61,1.395),.025,.045,'ShopBrass',(0,0,1),drawer)
    for j in range(8):ring('Drawer parts',(.90+(j%2)*.13,.578,1.62+(j//2)*.16),.028,.006,'ShopBrass',(0,1,0),drawer)
    # Repair tools are separate, sensibly sized meshes with physical thickness.
    cylinder('Magnifier base',(2.50,.857,1.75),.105,.025)
    beam('Magnifier articulated stem',(2.50,.87,1.75),(2.50,1.08,1.75),.017)
    ring('Magnifier brass lens rim',(2.50,1.14,1.72),.105,.013,'ShopBrass',(0,.65,-.75))
    cylinder('Magnifier glass',(2.50,1.14,1.72),.095,.006,'ShopGlass',(0,.65,-.75))
    cylinder('Task lamp foot',(2.84,.85,2.05),.12,.025)
    beam('Lamp lower arm',(2.84,.87,2.05),(2.82,1.20,2.05),.017)
    beam('Lamp upper arm',(2.82,1.2,2.05),(2.62,1.42,1.97),.017)
    cylinder('Green task shade',(2.62,1.40,1.97),.18,.12,'ShopGreen')
    cylinder('Task lamp glass',(2.62,1.335,1.97),.15,.012,'ShopGlow')
    for j in range(2):beam('Fine tweezer',(1.82+j*.025,.857,1.75),(1.96,.857,1.96),.004,'ShopIron')
    beam('Winding key stem',(2.14,.86,1.68),(2.22,.86,1.68),.012)
    for x in (2.13,2.23):ring('Winding key loop',(x,.86,1.68),.026,.006,'ShopBrass',(0,1,0))
    for j in range(6):
        radius=.035+(j%2)*.014;x=1.80+(j%3)*.14;z=1.88+(j//3)*.14
        ring('Watch movement wheel rim',(x,.861,z),radius*.80,.006,'ShopBrass',(0,1,0))
        cylinder('Watch wheel axle',(x,.861,z),.010,.012,'ShopBrass')
        for spoke in range(3):
            a=spoke*math.tau/3
            beam('Watch wheel spoke',(x,.861,z),(x+math.sin(a)*radius*.8,.861,z+math.cos(a)*radius*.8),.004,'ShopBrass')
        for n in range(12):
            a=n*math.tau/12;box('Wheel tooth',(x+math.sin(a)*radius,.858,z+math.cos(a)*radius),(.014,.008,.014),'ShopBrass',.001)
    # Clipboard and ledger intentionally contain no invented clue text.
    box('Notice clipboard',(1.1,.845,1.60),(.54,.026,.39),'ShopWalnut',.008)
    box('NoticePaper',(1.1,.863,1.60),(.50,.004,.35),'ShopPaper',.001)
    box('Clipboard brass clamp',(1.1,.878,1.75),(.13,.025,.045),'ShopBrass',.003)
    box('ServiceLedger',(1.50,.87,2.06),(.32,.055,.43),'ShopGreen',.008)
    box('Ledger page block',(1.50,.875,2.06),(.30,.035,.40),'ShopPaper',.002)
    for y in (.847,.903):box('Ledger leather cover',(1.50,y,2.06),(.33,.012,.43),'ShopGreen',.004)
    # A pocketwatch with a separately named open lid and solid crown.
    cylinder('Pocketwatch bezel',(2.30,.866,1.99),.065,.018,'ShopBrass')
    cylinder('Pocketwatch ivory dial',(2.30,.879,1.99),.057,.006,'ShopIvory')
    ring('Pocketwatch crown loop',(2.30,.878,2.065),.018,.005,'ShopBrass',(0,1,0))
    box('Pocketwatch lid hinge',(2.235,.874,1.99),(.012,.012,.03),'ShopBrass',.002)
    cylinder('Pocketwatch open lid',(2.178,.89,1.99),.065,.007,'ShopBrass',(1,.25,0))
    active_module=empty('Module_SoftFurnishings')
    box('Woven rug',(0,.035,-.6),(1.7,.012,3.1),'ShopRug',.002)
    for x in (-1.7,1.7):
        beam('Ceiling pendant cable',(x,3.3,-.55),(x,2.83,-.55),.009,'ShopIron')
        cylinder('Brass pendant shade',(x,2.81,-.55),.27,.07,'ShopBrass')
        cylinder('Pendant lit diffuser',(x,2.765,-.55),.20,.012,'ShopGlow')
    # Named calibration and seat points are retained through FBX export.
    empty('FrontMarker',(0,0,-4));empty('EastMarker',(3,0,0));empty('WorkbenchSeatMarker',(2,0,2.7))
    # Join per logical module/material, preserving movable fixtures and their pivots.
    bpy.context.view_layer.update()
    bad=[]
    for obj in meshes:
        low=min((obj.matrix_world@v.co).z for v in obj.data.vertices)
        if low<-.03:bad.append((obj.name,round(low,3),tuple(obj.location)))
    if bad:print('SOURCE_BELOW_FLOOR',bad[:8])
    for module in [o for o in list(collection.objects) if o.type=='EMPTY']:
        members=[o for o in list(module.children) if o.type=='MESH']
        groups={material.name:[o for o in members if o.data.materials[0]==material] for material in mats.values()}
        for material in mats.values():
            group=groups[material.name]
            if len(group)<2:continue
            bpy.ops.object.select_all(action='DESELECT')
            for obj in group:obj.select_set(True)
            bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();group[0].name=module.name+'_'+material.name
            bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=next(o for o in collection.objects if o.type=='MESH')
    bpy.ops.export_scene.fbx(filepath=str(art/'WalnutShop.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
    scene.world=bpy.data.worlds.new('Watch shop world');scene.world.use_nodes=True
    bpy.ops.wm.save_as_mainfile(filepath=str(source/'WalnutShop.blend'),compress=True)
    bpy.ops.wm.open_mainfile(filepath=str(source/'WalnutShop.blend'))
    parts=[o for o in bpy.data.collections['ShopVisual'].objects if o.type=='MESH']
    points=[o.matrix_world@v.co for o in parts for v in o.data.vertices]
    report={'blender':bpy.app.version_string,'sourceReopened':True,'parts':len(parts),'vertices':sum(len(o.data.vertices) for o in parts),
        'boundsMin':[min(p[i] for p in points) for i in range(3)],'boundsMax':[max(p[i] for p in points) for i in range(3)],
        'modules':[o.name for o in bpy.data.collections['ShopVisual'].objects if o.type=='EMPTY'],
        'provenance':'Original procedural geometry. No external asset textures. Selected user concept as style reference.',
        'unityRuntimeChecked':False}
    assert report['parts']<350 and report['vertices']<150000
    (source/'manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('WALNUT_SHOP_SOURCE_PASS',json.dumps(report))


if __name__=='__main__':main()
