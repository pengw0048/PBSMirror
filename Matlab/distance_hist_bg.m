%% 读文件
filename = 'D:\wifi\dist_bg.dat';
delimiter = '';
formatSpec = '%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
dist = dataArray{:, 1};
clearvars filename delimiter formatSpec fileID dataArray ans;

%% 画距离直方图
figure
hold on
set(gcf,'position',[200,200,400,300])
box on
[N,edges]=histcounts(log(dist),80);
for i=1:size(N,2)
    fill([exp(edges(i)),exp(edges(i+1)),exp(edges(i+1)),exp(edges(i))],[1,1,N(i),N(i)],'b')
end
set(gca,'yscale','log')
set(gca,'xscale','log')
ylim=get(gca,'ylim');
set(gca,'ylim',[1,ylim(2)])
set(gca,'xlim',[1,10^8])
xlabel('百度和Google定位结果的误差（米）')
ylabel('数量')
hold off
