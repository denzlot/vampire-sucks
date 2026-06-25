# Конвертация рига GLB -> FBX (с костями и анимацией) через Blender.
# Запуск (Windows, из папки проекта):
#   "C:\Program Files\Blender Foundation\Blender 4.x\blender.exe" --background --python glb_to_fbx.py -- Assets\Models\runa_hand_rigged.glb Assets\Models\runa_hand_rigged.fbx
# После этого Unity увидит обычный FBX: вкладка Rig -> Animation Type = Generic,
# анимация ClawSwipe внутри. Текстуру вешаешь своим материалом, как на остальных персонажах.
import bpy, sys

argv = sys.argv[sys.argv.index("--") + 1:]
src = argv[0]
dst = argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

bpy.ops.export_scene.fbx(
    filepath=dst,
    object_types={'ARMATURE', 'MESH'},
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    add_leaf_bones=False,
    apply_unit_scale=True,
    path_mode='COPY',
)
print("FBX saved to", dst)
