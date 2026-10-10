"""Supported, editable ticket-office joinery in Unity metres; no story-bearing print."""
import json
import math

WOOD = 'RailOfficeWalnut'
TOP = 1.15


def add_ticket_office(b):
    contacts = []
    fixtures = {}

    def fixture(name, pos, size, mat=WOOD, bevel=.003):
        obj = b.box(name, pos, size, mat, bevel=bevel, uv_scale=.75)
        obj['ticket_support_fixture'] = True
        fixtures[name] = obj
        return obj

    def rest(name, support):
        contacts.append({'item': name, 'support': support})

    b.set_module('Module_TicketOffice_Joinery')
    fixture('TicketCounter', (-8,.6,1), (5,1,.90))
    fixture('TicketCounterTop', (-8,1.12,1), (5.2,.06,1.05))
    rest('TicketCounterTop', 'TicketCounter')
    b.box('OfficePlinth', (-8,.17,.99), (5.10,.14,.93), WOOD)
    b.box('OfficeKickplate', (-8,.17,.517), (5.02,.13,.018), 'RailIron')
    b.box('CounterBullnose', (-8,1.12,.476), (5.2,.045,.028), WOOD, bevel=.012)
    b.box('CounterBrassWearStrip', (-8,1.151,.51), (5.03,.002,.018), 'RailBrass', bevel=0)
    for i,x in enumerate((-9.5,-8,-6.5)):
        b.box(f'OfficeFrontPanel_{i}', (x,.62,.536), (1.26,.70,.025), 'RailGreen')
        for y in (.24,.99):
            b.box(f'OfficePanelRail_{i}_{y}', (x,y,.511), (1.34,.05,.042), WOOD)
        for sx in (-1,1):
            b.box(f'OfficePanelStile_{i}_{sx}', (x+sx*.65,.615,.511), (.05,.70,.042), WOOD)
        for y in (.30,.93):
            b.box(f'OfficeInsetBead_{i}_{y}', (x,y,.513), (1.19,.015,.018), 'RailBrass', bevel=.001)
        for sx in (-1,1):
            b.box(f'OfficeInsetBeadSide_{i}_{sx}', (x+sx*.59,.615,.513), (.015,.615,.018), 'RailBrass', bevel=.001)
    # Pilasters bear the entire upper screen and cornice, rather than hanging in air.
    for i,x in enumerate((-10.48,-8.75,-7.25,-5.52)):
        name=f'OfficePost_{i}'
        fixture(name, (x,1.28,1.06), (.13,2.36,.16))
        b.box(f'OfficePostFoot_{i}', (x,.21,1.04), (.20,.22,.22), WOOD)
        b.box(f'OfficePostCapital_{i}', (x,2.42,1.06), (.24,.08,.23), WOOD)
        for off in (-.026,.026):
            b.box(f'OfficePostFlute_{i}_{off}', (x+off,1.84,.974), (.012,1.02,.008), 'RailBrass', bevel=.001)
    fixture('OfficeHeaderBeam', (-8,2.49,1.06), (5.16,.06,.19))
    for i in range(4):
        rest('OfficeHeaderBeam',f'OfficePost_{i}')
    fixture('SignBacking_Tickets', (-8,2.70,1.05), (5.16,.36,.19), 'RailGreen')
    rest('SignBacking_Tickets','OfficeHeaderBeam')
    fixture('OfficeCorniceLower', (-8,2.91,1.05), (5.30,.06,.28))
    rest('OfficeCorniceLower','SignBacking_Tickets')
    b.box('OfficeCorniceCrown', (-8,2.977,1.06), (5.42,.074,.32), WOOD, bevel=.008)
    for y in (2.551,2.849):
        b.box('HeaderBrassRule'+str(y), (-8,y,.951), (4.96,.012,.008), 'RailBrass', bevel=.001)
    for x in (-10.39,-5.61):
        b.cylinder('HeaderEndRosette'+str(x), (x,2.70,.947), .058,.012,'RailBrass',axis=(0,0,1),vertices=16)

    # Three framed screens with an ACTUAL open-bottom arched service aperture.
    b.set_module('Module_TicketOffice_Grilles')
    for i,x in enumerate((-9.5,-8,-6.5)):
        left,right=x-.57,x+.57
        for sx in (left,right):
            b.box(f'GrilleJamb_{i}_{sx}', (sx,1.78,1.065), (.045,1.26,.055),'RailBrass')
        b.box(f'GrilleTop_{i}', (x,2.39,1.065), (1.185,.04,.055),'RailBrass')
        # Aperture starts at countertop, straight jambs to 1.50, arch apex 1.80.
        for sx in (-1,1):
            b.beam(f'WicketSide_{i}_{sx}',(x+sx*.30,TOP,1.065),(x+sx*.30,1.50,1.065),.014,'RailBrass')
        for k in range(20):
            a=k*math.pi/20; c=(k+1)*math.pi/20
            b.beam(f'WicketArch_{i}_{k}',(x+.30*math.cos(a),1.50+.30*math.sin(a),1.065),
                   (x+.30*math.cos(c),1.50+.30*math.sin(c),1.065),.014,'RailBrass',vertices=8)
        for j in range(11):
            dx=-.5+j*.1
            bottom=1.16 if abs(dx)>.315 else 1.50+math.sqrt(max(0,.30**2-dx**2))+.014
            b.beam(f'GrilleBar_{i}_{j}',(x+dx,bottom,1.065),(x+dx,2.37,1.065),.006,'RailBrass',vertices=8)
        for y in (1.94,2.19):
            b.beam(f'GrilleCrossbar_{i}_{y}',(left,y,1.065),(right,y,1.065),.007,'RailBrass',vertices=8)
        for dx in (-.4,0,.4):
            b.ring(f'GrilleDiamond_{i}_{dx}',(x+dx,2.055,1.065),.067,.0045,'RailBrass',segments=4,ring_segs=6)
        # Shallow transaction tray has its own grounded base; it doesn't cover the opening.
        fixture(f'WicketTrayBase_{i}', (x,TOP+.006,.80), (.43,.012,.31),'RailBrass',bevel=.002)
        rest(f'WicketTrayBase_{i}','TicketCounterTop')
        for sx in (-1,1):
            b.box(f'WicketTraySide_{i}_{sx}', (x+sx*.216,1.171,.80), (.012,.03,.31),'RailBrass')
        b.box(f'WicketTrayBack_{i}',(x,1.171,.951),(.43,.03,.012),'RailBrass')

    # Back storage: continuously grounded plinth, lower cabinet and fixed pigeonhole top.
    # Storage ends at z2.96; the clerk still has 0.86m between it and the counter.
    b.set_module('Module_TicketOffice_Storage')
    fixture('OfficeStorageBase', (-8,.65,2.71), (4.40,1.10,.48))
    fixture('OfficeStorageWorktop',(-8,1.24,2.71),(4.48,.08,.53))
    rest('OfficeStorageWorktop','OfficeStorageBase')
    fixture('TicketPigeonholeBack',(-8,2.08,2.93),(4.4,1.60,.04))
    rest('TicketPigeonholeBack','OfficeStorageWorktop')
    for side in (-1,1):
        name='TicketPigeonholeSide'+str(side)
        fixture(name,(-8+side*2.18,2.08,2.72),(.04,1.60,.46))
        rest(name,'OfficeStorageWorktop')
    for row,y in enumerate((1.30,1.60,1.90,2.20,2.50,2.87)):
        fixture(f'PigeonShelf_{row}',(-8,y,2.72),(4.32,.04,.44))
        for col in range(8):
            x=-9.68+col*.48
            if row<5:
                b.box(f'PigeonDivider_{row}_{col}',(x+.24,(y+(1.60,1.90,2.20,2.50,2.87)[row])/2,2.72),
                      (.02,(1.60,1.90,2.20,2.50,2.87)[row]-y,.44),WOOD,bevel=.001)
            if (row*7+col*3)%5<2 and row<5:
                h=.06+((row+col)%3)*.028
                name=f'PigeonDocBundle_{row}_{col}'
                fixture(name,(x,y+.02+h/2,2.66),(.32,h,.28),'RailIvory',bevel=.001)
                rest(name,f'PigeonShelf_{row}')
                b.box(name+'Wrapper',(x,y+.02+h+.0015,2.66),(.33,.003,.285),('RailGreen','RailRed',WOOD)[col%3],bevel=.001)
                b.box(name+'Tie',(x,y+.02+h+.004,2.66),(.028,.002,.28),'RailIvory',bevel=0)
    # Drawer fronts inset into the cabinet, with mounting plates and handles.
    for col in range(4):
        x=-9.63+col*1.085
        for row in range(3):
            y=.31+row*.29
            b.box(f'StockDrawer_{row}_{col}',(x,y,2.458),(1.025,.265,.023),'RailGreen')
            b.box(f'DrawerLabelPlate_{row}_{col}',(x,y+.043,2.442),(.16,.055,.009),'RailBrass',bevel=.001)
            for dx in (-.055,.055):
                b.cylinder(f'DrawerKnobMount_{row}_{col}_{dx}',(x+dx,y-.05,2.435),.016,.02,'RailBrass',axis=(0,0,1),vertices=8)
            b.beam(f'DrawerPull_{row}_{col}',(x-.055,y-.05,2.425),(x+.055,y-.05,2.425),.009,'RailBrass',vertices=8)
    # End cheeks join front to back; right service passage stays open (0.88m).
    b.set_module('Module_TicketOffice_EndPanels')
    fixture('OfficeLeftCheek',(-10.48,.65,1.86),(.13,1.10,1.46))
    b.box('OfficeLeftCheekTop',(-10.48,1.24,1.86),(.20,.08,1.49),WOOD)
    b.box('OfficeEndGreenPanel',(-10.551,.65,1.86),(.015,.84,1.23),'RailGreen')
    # Low brass customer rail supported by four feet/brackets fixed to the counter.
    for x in (-10.20,-8.75,-7.25,-5.80):
        b.cylinder('FootrailFoot'+str(x),(x,.119,.34),.055,.038,'RailBrass',axis=(0,1,0),vertices=12)
        b.beam('FootrailStem'+str(x),(x,.138,.34),(x,.32,.34),.013,'RailBrass')
        b.beam('FootrailMount'+str(x),(x,.32,.34),(x,.32,.52),.013,'RailBrass')
    b.beam('CounterFootRail',(-10.20,.32,.34),(-5.80,.32,.34),.021,'RailBrass')

    # Tools are placed from their supporting surface, not arbitrary absolute heights.
    b.set_module('Module_TicketOffice_DeskProps')
    fixture('OfficeBlotter',(-8.50,TOP+.004,.82),(.48,.008,.34),'RailFelt',bevel=.002)
    rest('OfficeBlotter','TicketCounterTop')
    fixture('InkstandTray',(-8.50,1.166,.85),(.30,.016,.16))
    rest('InkstandTray','OfficeBlotter')
    for i,x in enumerate((-8.58,-8.42)):
        b.cylinder(f'InkwellPot_{i}',(x,1.192,.88),.030,.036,'RailBrass',axis=(0,1,0),vertices=12)
        b.cylinder(f'InkwellLid_{i}',(x,1.215,.88),.024,.010,'RailBrass',axis=(0,1,0),vertices=12)
    b.box('PenRest',(-8.5,1.178,.806),(.24,.008,.028),'RailBrass',bevel=.001)
    b.beam('DeskFountainPen',(-8.61,1.186,.806),(-8.40,1.186,.806),.004,'RailIron',vertices=8)
    # Bell bell-shaped shell and spring plunger, assembled on the countertop.
    fixture('CashierBellBase',(-7.28,1.159,.76),(.17,.018,.17),'RailBrass',bevel=.007)
    rest('CashierBellBase','TicketCounterTop')
    for j in range(6):
        radius=.068*math.sqrt(max(.08,1-(j/6)**2))
        b.cylinder(f'CashierBellDome_{j}',(-7.28,1.173+j*.009,.76),radius,.01,'RailBrass',vertices=20)
    b.cylinder('CashierBellButton',(-7.28,1.237,.76),.015,.031,'RailBrass',vertices=12)
    # Closed ledger: two covers and a visible paper fore-edge, no duplicate overlapping books.
    fixture('SafeLedgerCover',(-9.96,1.153,1.20),(.32,.006,.40),'RailRed',bevel=.002)
    rest('SafeLedgerCover','TicketCounterTop')
    b.box('SafeLedgerPages',(-9.955,1.174,1.20),(.30,.036,.38),'RailIvory',bevel=.001)
    b.box('SafeLedgerTopCover',(-9.96,1.195,1.20),(.32,.006,.40),'RailRed',bevel=.002)
    b.box('LedgerSpine',(-10.119,1.174,1.20),(.012,.040,.40),'RailRed')
    for side in (-1,1):
        b.box('LedgerCorner'+str(side),(-9.823,1.199,1.20+side*.18),(.035,.002,.035),'RailBrass',bevel=.001)
    # A single-hole plier rests on its lower flat handle, with pivot, spring and open jaws.
    fixture('PunchFixedHandle',(-6.69,1.162,.60),(.23,.024,.036),'RailIron',bevel=.006)
    rest('PunchFixedHandle','TicketCounterTop')
    b.beam('PunchUpperHandle',(-6.80,1.183,.60),(-6.59,1.203,.60),.011,'RailIron',vertices=12)
    b.cylinder('PunchPivot',(-6.595,1.191,.60),.019,.042,'RailBrass',axis=(0,0,1),vertices=16)
    b.box('PunchLowerJaw',(-6.554,1.179,.60),(.06,.011,.036),'RailIron',bevel=.002)
    b.box('PunchUpperJaw',(-6.554,1.212,.60),(.06,.011,.036),'RailIron',bevel=.002)
    b.cylinder('PunchDie',(-6.545,1.200,.60),.0045,.015,'RailIron',vertices=12)
    b.beam('PunchReturnSpring',(-6.735,1.176,.60),(-6.685,1.199,.60),.003,'RailBrass',vertices=6)
    # Small enamel-shaded work lamps provide a warm pool on the writing surface.
    # Their bases and stems are attached; Unity binds light and emission together.
    b.set_module('Module_TicketOffice_WorkLamps')
    lamps=[]
    for i,x in enumerate((-9.1,-6.9)):
        name=f'OfficeLampBase_{i}'
        fixture(name,(x,1.159,1.35),(.16,.018,.12),'RailBrass',bevel=.007)
        rest(name,'TicketCounterTop')
        b.beam(f'OfficeLampStem_{i}',(x,1.168,1.37),(x,1.52,1.37),.009,'RailBrass')
        b.beam(f'OfficeLampBracket_{i}',(x,1.52,1.37),(x,1.52,1.295),.009,'RailBrass')
        for radius,mat,suffix in ((.085,'RailGreen','Enamel'),(.081,'RailIvory','Inner')):
            pts=[];faces=[]
            for xx in (x-.15,x+.15):
                pts += [(xx,1.52+radius*math.sin(j*math.pi/12),1.22+radius*math.cos(j*math.pi/12)) for j in range(13)]
            faces=[(j,j+1,j+14,j+13) for j in range(12)]
            faces.extend([tuple(range(12,-1,-1)),tuple(range(13,26))])
            b.authored_mesh(f'OfficeLampShade_{i}_{suffix}',pts,faces,mat)
        for side in (-1,1):
            b.beam(f'OfficeLampRim_{i}_{side}',(x-.15,1.52,1.22+side*.085),(x+.15,1.52,1.22+side*.085),.004,'RailBrass',vertices=8)
        glow=f'OfficeLampBulb_{i}'
        obj=b.cylinder(glow,(x,1.532,1.22),.013,.25,'RailGlow',axis=(1,0,0),vertices=12)
        obj['ticket_support_fixture']=True
        for side in (-1,1):
            b.cylinder(f'OfficeLampSocket_{i}_{side}',(x+side*.1375,1.532,1.22),.021,.025,'RailBrass',axis=(1,0,0),vertices=10)
        lamps.append({'bulb':glow,'position':[x,1.495,1.22]})
    # Discarded, illegible scraps form a continuous pile from the basket floor.
    # Readable stock is added separately in Unity with three real perforations.
    b.set_module('Module_TicketBin_PaperFill')
    obj=b.cylinder('DiscardedPaperCore',(-11.3,.403,-.8),.202,.586,'RailIvory',vertices=18)
    fixtures[obj.name]=obj;obj['ticket_support_fixture']=True
    obj=b.collection.objects['TicketBin_InnerBottom'];fixtures[obj.name]=obj;obj['ticket_support_fixture']=True
    rest('DiscardedPaperCore','TicketBin_InnerBottom')
    for layer in range(8):
        y=.12+layer*.071
        for side in range(6):
            a=side*math.tau/6+layer*.41
            cx=-11.3+.15*math.cos(a);cz=-.8+.15*math.sin(a)
            pts=[(cx-.073,y,cz-.056),(cx+.063,y+.006,cz-.055),(cx+.075,y+.018,cz+.051),(cx-.062,y+.011,cz+.059),
                 (cx-.069,y+.044,cz-.049),(cx+.059,y+.059,cz-.048),(cx+.067,y+.067,cz+.043),(cx-.057,y+.052,cz+.051)]
            b.authored_mesh(f'CrumpledDiscard_{layer}_{side}',pts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6),(4,6,7)],'RailIvory')
    spent=[(-.035,-.10,-15),(.080,.035,54),(-.10,.06,-28),(.04,.10,112),
           (-.02,.005,28),(.09,-.065,-48),(-.12,-.06,80),(.01,.025,0)]
    points=[];faces=[];ticket_poses=[]
    for i,(dx,dz,yaw) in enumerate(spent):
        y=.716+i*.00035
        ticket_poses.append({'x':dx,'z':dz,'yaw':yaw,'supportTop':y})
        cx=-11.3+dx;cz=-.8+dz;k=len(points)
        # A folded paper ridge bears the centre of each loose, separately visible ticket.
        points += [(cx-.047,.696,cz-.035),(cx+.047,.696,cz-.035),(cx+.047,.696,cz+.035),(cx-.047,.696,cz+.035),
                   (cx-.018,y,cz-.013),(cx+.018,y,cz-.013),(cx+.018,y,cz+.013),(cx-.018,y,cz+.013)]
        faces += [tuple(k+j for j in face) for face in [(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]]
    obj=b.authored_mesh('WastePaperPileSupport',points,faces,'RailIvory')
    fixtures[obj.name]=obj;obj['ticket_support_fixture']=True
    rest('WastePaperPileSupport','DiscardedPaperCore')
    # Geometry and contact contract is measured from the realised meshes before batching.
    b.scene.view_layers[0].update()
    for pair in contacts:
        item=fixtures[pair['item']];support=fixtures[pair['support']]
        bottom=min((item.matrix_world@v.co).z for v in item.data.vertices)
        top=max((support.matrix_world@v.co).z for v in support.data.vertices)
        pair['gapMetres']=round(bottom-top,6)
        if abs(bottom-top)>.011:
            raise ValueError(f"Floating/joinery overlap: {pair}: {bottom-top}")
    return {'version':1,'lamps':lamps,'spentTicketPoses':ticket_poses,'floorTop':.10,'counterTop':TOP,'contacts':contacts,
            'grounded':['TicketCounter','OfficeStorageBase','OfficeLeftCheek','OfficePost_0','OfficePost_1','OfficePost_2','OfficePost_3','TicketBin_InnerBottom'],
            'fixtures':sorted(fixtures),'style':'Original timber and brass booth inspired by Station B; no invented dates or schedules.'}
