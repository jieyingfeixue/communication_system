-- 初始化管理员账号（密码: admin123，BCrypt 加密）
-- 若已存在 admin 用户可跳过
INSERT INTO `User` (`Username`, `PasswordHash`, `Nickname`, `Status`, `Role`)
SELECT 'admin', '$2a$11$8K1p/a0dL1LXMIgoEDFrwOfMQdM1lQKxvJJqVxT3p8zqGxWvF5gK.', '系统管理员', 1, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `User` WHERE `Username` = 'admin');
