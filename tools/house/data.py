from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'game/Assets/Art/House1931'; SRC=ROOT/'assets/source/house'
# Stable player-facing records. No automatic deductions or hidden perpetrator attribution.
records=[
('H01','现场查阅与清点单','住宅玄关 · 警方留置副本','住宅已完成初步勘查。家属可凭查阅许可进入，登记材料并核对副本。\n\n旧外套、工具牌、机油手套与现场残信已扣押；对应原位置留有编号。铁柜内文书已清点，非扣押家属文件归还原处后重锁。'),
('E02','完整车票','主角随身的原件','1931年1月20日。\n\n纸面完整，边缘没有撕裂痕迹，票面没有检票孔。'),
('H03','旧外套的扣押照片','玄关衣钩 · 警方照片副本','编号03：旧外套，取自玄关衣钩。袖口磨损，衣摆留有暗色污痕。\n\n照片不附污痕成分检验结果。原件已封存。'),
('H04','会客室现场记录','会客室 · 警方现场记录副本','死者发现于茶几与侧柜之间。现场留有轮廓标记。\n\n遗体已移送。现有记录只能将死亡时间估为较宽的傍晚时段。'),
('H05','失窃申报','会客室收款抽屉 · 家属陈述副本','申报物：家庭现金，约一百五十元。\n申报人：丈夫。\n\n收款抽屉处于打开状态。数额据申报人口述，尚未附现金盘点凭证。'),
('H06','现场残信与物件照片','会客室档案夹 · 扣押物照片','照片中有工具牌、机油手套及一片信纸。\n\n信上可读：“……再这样拖下去，我不会放过你……”\n纸张上下两端缺失，原件已经封存。'),
('E09','残信裁余片','书房书桌抽屉内侧','同一张薄信纸的上下两片。上片末行：“修车铺那笔钱，你答应过要还。”\n下片首行：“……欠下的账。我会去找你把话说清楚。二十一日早班车的事，另信再谈。”\n\n裁边、行距与案卷残信照片可供比对。'),
('E10','病房来信','书房桌面 · 护士寄给家属的信','今天她愿意多吃了几口。您太太带来一篮苹果，挑出软一些的，切成了小块。她坐到探视时间结束，篮子留在这里，自己一个也没有拿。\n\n请下次带两件干净内衣。——病房护士'),
('H09','医院月结账单','书房医疗文件夹 · 家属留存联','患者资料｜住院号 C-017\n出生日期：1922年6月14日\n\n账单日期：1931年1月15日\n项目：住院护理、换药及生活照护\n本期应付：三十八元。前期未清：六十二元。\n\n本联请由家属保存。'),
('H10','护理续费催缴函','书房医疗文件夹','前期护理费用尚有欠款。请家属至缴费窗口核对分期安排。\n\n长期照护仍需继续。本函不替代医嘱。'),
('H11','保险柜清点回执','保险柜内沿 · 警方清点回执','所见：保单、缴费凭据、家庭存取款凭据及家用信札。\n\n已摘录登记。此批家属文书返还柜内，清点后重锁；涉案扣押物另列，不在此柜。'),
('H12','妻子的意外保险单','保险柜 · 家庭保单原件','生效：1928年12月。\n被保险人：妻子。受益人：丈夫。\n\n后附历次缴费凭据。\n保险责任须按条款审查；受益人参与伤害、故意行为及虚假陈述不得据此当然获赔。'),
('H13','存取款凭据','保险柜 · 家属留存凭据','1931年1月19日，取款四十元。\n同日，另一笔取款六十元。\n经手：妻子。\n\n两张留存联夹在家庭账页中，未记这些现金后来的去向。'),
('H14','夫妻旧纸条','保险柜 · 家用信札','医院的账由你去核。我今天把煤钱补上。孩子的旧毛衣不要扔，袖口还能拆下来补。\n\n背面另有一行：“钱不够的时候，至少先同我说一声。”'),
('H15','食品与煤炭催账','厨房餐柜 · 店铺账单','面粉、茶叶、糖与煤炭的赊欠分列在不同纸上。\n\n有两张写着“已收部分”，另有一张请下次送货前结清旧账。'),
('H16','赌债催款便条','厨房杂账夹后','你说这个星期会把剩下的钱还上。我不能总替你垫着。\n\n这次请给个准信。——酒馆账房'),
('H17','厨房门锁记录','西厨房门 · 现状与勘查摘录','锁舌和门框没有明显被撬裂的破口。内侧有两道深浅不同的旧痕。\n\n记录没有确定划痕形成的日期。'),
('H18','厨房挂钟','厨房墙面','旧木壳挂钟。玻璃内侧有一层薄灰，摆锤停在一侧。'),
('H19','洗衣与修补收据','后翼杂务房','几张普通衣物清洗和修补收据，日期各不相同。\n\n袖口、裤脚和被套多次出现在修补项目里。'),
('H20','情夫来信','夫妻卧室 · 梳妆台信匣','二十一日，早班车。\n\n我会在车站等。不要带太多行李，那只小箱子就够了。你说钱的事已经安排好，我希望这次是真的。\n\n你说会把家里的事讲清楚。我等你的回信。'),
('H21','未寄出的回信与行李单','夫妻卧室 · 围巾下','我已经开始收东西。你别再到这里来，见面的话到车站说。\n\n附页：换洗衣物、围巾、梳子、信匣、现金。\n\n信没有贴邮票，末尾尚未写完。'),
('H22','家用便笺','夫妻卧室 · 记事本','柜子的数还用孩子生日？\n月份在前，日子在后，别再反过来。\n\n钥匙扣的线松了，我放在床头小抽屉里。'),
('H23','典当存根','夫妻卧室 · 首饰盒','首饰盒里只留下一只耳环和几枚旧扣子。\n\n存根写着一条项链、一枚胸针；具体结算需凭此联到当铺核对。'),
('H24','旧生日卡','孩子旧卧室 · 玩具柜','六月十四日。\n\n愿你今年也能把所有蜡烛一口气吹灭。\n\n卡片边上画着三个人和一座房子。'),
('H25','旧照片和儿童画','孩子旧卧室 · 书桌','相框里是一家三口的合照。孩子站在父母中间，手里握着一只木头小马。\n\n旁边的画背面写着“1928年5月”，没有具体日。'),
('H26','照护笔记','孩子旧卧室 · 床头抽屉','换洗衣物。把苹果切小。冬天记得带厚袜子。\n\n几页上有两种不同的字迹，部分事项被划掉又添上；有一页折角已经磨破。')]
ART.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
entries=[dict(id=i,title=t,source=s,body=b) for i,t,s,b in records]
(ROOT/'game/Assets/Resources/HouseEvidence.json').write_text(json.dumps({'entries':entries},ensure_ascii=False,indent=2))
(SRC/'research.json').write_text(json.dumps({'date':'2026-10-12','setting':'Fictional town, January 1931. Primarily British interwar domestic references; no country canon inferred.','sources':[{'url':'https://www.nationaltrust.org.uk/visit/nottinghamshire-lincolnshire/mr-straws-house','use':'Preserved 1923 grocer family house: older furniture, ordinary possessions, hall coats and mantel photographs.'},{'url':'https://www.vam.ac.uk/articles/art-deco-in-the-home','use':'1920s furnishing textiles; 1930-31 geometric tea services. Later 1932 radio and 1934 heater deliberately excluded.'},{'url':'https://www.nhbc.co.uk/kontentdocuments/9ba93546-3904-40d9-8ebb-28b64afa3f58/homes-through-the-decades.pdf','use':'Interwar layout context; PDF screenshot fetch unavailable, not used as a traced plan.'}],'art':'Original geometry and procedural textures; sources used for reference, no museum photographs used as game textures.'},ensure_ascii=False,indent=2))
