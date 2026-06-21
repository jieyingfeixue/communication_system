/*
 Navicat Premium Dump SQL

 Source Server         : communication_system
 Source Server Type    : MySQL
 Source Server Version : 80046 (8.0.46)
 Source Host           : 124.222.131.187:3306
 Source Schema         : chat_db

 Target Server Type    : MySQL
 Target Server Version : 80046 (8.0.46)
 File Encoding         : 65001

 Date: 20/06/2026 11:35:03
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for Friendship
-- ----------------------------
DROP TABLE IF EXISTS `Friendship`;
CREATE TABLE `Friendship`  (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `UserId1` int NOT NULL COMMENT '用户1的Id',
  `UserId2` int NOT NULL COMMENT '用户2的Id',
  `Status` tinyint NOT NULL DEFAULT 0 COMMENT '状态：0-申请中, 1-已是好友',
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '申请/建立时间',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `ux_user1_user2`(`UserId1` ASC, `UserId2` ASC) USING BTREE,
  INDEX `UserId2`(`UserId2` ASC) USING BTREE,
  CONSTRAINT `Friendship_ibfk_1` FOREIGN KEY (`UserId1`) REFERENCES `User` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `Friendship_ibfk_2` FOREIGN KEY (`UserId2`) REFERENCES `User` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '好友关系表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for Group
-- ----------------------------
DROP TABLE IF EXISTS `Group`;
CREATE TABLE `Group`  (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `GroupName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '群名称',
  `OwnerId` int NOT NULL COMMENT '群主UserId',
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `OwnerId`(`OwnerId` ASC) USING BTREE,
  CONSTRAINT `Group_ibfk_1` FOREIGN KEY (`OwnerId`) REFERENCES `User` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '群组表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for GroupMember
-- ----------------------------
DROP TABLE IF EXISTS `GroupMember`;
CREATE TABLE `GroupMember`  (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `GroupId` int NOT NULL COMMENT '群组Id',
  `UserId` int NOT NULL COMMENT '群成员用户Id',
  `JoinTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '进群时间',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `ux_group_user`(`GroupId` ASC, `UserId` ASC) USING BTREE,
  INDEX `UserId`(`UserId` ASC) USING BTREE,
  CONSTRAINT `GroupMember_ibfk_1` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `GroupMember_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `User` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '群成员表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for Message
-- ----------------------------
DROP TABLE IF EXISTS `Message`;
CREATE TABLE `Message`  (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '消息主键(大作业消息多，用BIGINT防溢出)',
  `SenderId` int NOT NULL COMMENT '发送人Id',
  `ReceiverId` int NULL DEFAULT NULL COMMENT '接收人Id（私聊时有效，群聊时为NULL）',
  `GroupId` int NULL DEFAULT NULL COMMENT '群ID（群聊时有效，私聊时为NULL）',
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '消息文本内容或文件、图片路径',
  `MessageType` tinyint NOT NULL DEFAULT 0 COMMENT '消息类型：0-文本, 1-图片, 2-文件',
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '发送时间',
  `IsRead` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否已读：0-未读, 1-已读',
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `SenderId`(`SenderId` ASC) USING BTREE,
  INDEX `ReceiverId`(`ReceiverId` ASC) USING BTREE,
  INDEX `GroupId`(`GroupId` ASC) USING BTREE,
  CONSTRAINT `Message_ibfk_1` FOREIGN KEY (`SenderId`) REFERENCES `User` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `Message_ibfk_2` FOREIGN KEY (`ReceiverId`) REFERENCES `User` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `Message_ibfk_3` FOREIGN KEY (`GroupId`) REFERENCES `Group` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '历史消息表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for User
-- ----------------------------
DROP TABLE IF EXISTS `User`;
CREATE TABLE `User`  (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `Username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '唯一账号',
  `PasswordHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '加盐哈希加密密码',
  `Nickname` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '昵称',
  `Avatar` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '头像文件路径',
  `Status` tinyint NOT NULL DEFAULT 0 COMMENT '状态：0-待审批, 1-正常, 2-已被禁用',
  `Role` tinyint NOT NULL DEFAULT 0 COMMENT '角色：0-一般用户, 1-管理员',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `Username`(`Username` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '用户表' ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
