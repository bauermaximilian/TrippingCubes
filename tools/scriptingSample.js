// This file contains some ideas how a future script in GameCraft could look 
// like - so it's just a draft and will probably change during implementation.

controller.setPosition(0, 0, 0);
controller.setCollider(1, 1.7, 1, "CAPSULE");
controller.setGravity(-9.81);
controller.disableUserInput();

var tree = world.createModel(10);
tree.setVoxel(5, 0, 5, "WOOD", "UP");
tree.setVoxel(5, 1, 5, "WOOD", "UP");
tree.setVoxel(5, 2, 5, "WOOD", "UP");
tree.setVoxel(5, 3, 5, "WOOD", "UP");
tree.setVoxel(5, 4, 5, "WOOD", "UP");
tree.setVoxel(5, 5, 5, "WOOD", "UP");
tree.setPivot(5, 0, 5);

// x,y,z,sx,sy,sz,block-id
world.setVoxels(0, 0, 0, 25, 1, 25, "GRASS");
world.setVoxels(5, 1, 5, 5, 1, 1, "FENCE", "X");
world.setVoxels(10, 1, 5, 5, 1, 1, "FENCE", "Y");
world.insertModel(5, 5, 5, tree);

controller.enableUserInput();
controller.setControlMode("EDITOR");
controller.setGravity(0);

// function setCinematicCamera() {...}

// Take absolute position, remove current chunk offset, make modifications 
// over current chunk? maybe additional editor cursor after old concept? e.g.
var brush = world.createBrush();
brush.moveTo(5, 0, 5);
brush.setBlockType("FENCE");
brush.setBlockVariation("X-ALIGNED");
brush.fillArea(10, 1, 1, true);
brush.move(0, 1, 0);
brush.setBlockType("TORCH");
brush.setBlockVariation("STANDALONE");
brush.setVoxel();

var tree = createModel(10);
var treeBrush = tree.createBrush();

// Ideas from Worldedit
undo();
redo();
// See https://minecraft-worldedit.fandom.com/wiki/Worldedit_Commands
// and https://github.com/sebastienros/jint