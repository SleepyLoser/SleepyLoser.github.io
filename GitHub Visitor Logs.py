import requests
import datetime
import time
import os

# 配置参数（需用户自行修改）
HEAD_PATH = "D:/Work/GitHub/Warehouse Visitor Log/"
TOKEN = "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"  # 需申请repo权限
REPO_OWNER = "SleepyLoser"
REPO_NAME = ["UnityGif", "SleepyLoser.github.io"]
SAVE_PATH = ["UnityGif Records/log.txt", "Blog Records/log.txt"]  # 日志保存路径

def get_repo_visitors(repo_name, save_path):
    headers = {
        "Authorization": f"token {TOKEN}",
        "Accept": "application/vnd.github.v3+json"
    }
    url = f"https://api.github.com/repos/{REPO_OWNER}/{repo_name}/traffic/views"
    
    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        data = response.json()
        
        # 提取关键数据
        timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        unique_visitors = data.get('uniques', 0)
        total_views = data.get('count', 0)
        
        # 写入日志文件
        log_entry = f"{timestamp} | 独立访客: {unique_visitors} | 总访问量: {total_views}\n"
        with open(save_path, 'a') as f:
            f.write(log_entry)
            
        print(f"记录成功：{log_entry.strip()}")
        
    except requests.exceptions.HTTPError as err:
        print(f"API请求失败, 状态码：{err.response.status_code}")
    except Exception as e:
        print(f"发生错误：{str(e)}")

if __name__ == "__main__":
    # 创建日志文件目录（如果不存在）
    # os.makedirs(os.path.dirname(SAVE_PATH), exist_ok=True)
    
    # 每天记录一次（可调整时间间隔）
    # while True:
    #     get_repo_visitors()
    #     time.sleep(86400)  # 86400秒=24小时

    for index in range(len(REPO_NAME)) :
        save_path = HEAD_PATH + SAVE_PATH[index]
        os.makedirs(os.path.dirname(save_path), exist_ok=True)
        get_repo_visitors(REPO_NAME[index], save_path)