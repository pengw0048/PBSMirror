%% 读文件
filename = 'D:\wifi\signal.dat';
delimiter = '';
formatSpec = '%f%[^\n\r]';
fileID = fopen(filename,'r');
dataArray = textscan(fileID, formatSpec, 'Delimiter', delimiter, 'EmptyValue' ,NaN, 'ReturnOnError', false);
fclose(fileID);
signal = dataArray{:, 1};
clearvars filename delimiter formatSpec fileID dataArray ans;

for i=1:size(signal,1)
    if signal(i)>0
        signal(i)=signal(i)*2-113;
    end
end
signal=signal(signal<-1);
signal=signal(signal>-99);
signal=signal(signal~=0);

%% 画距离直方图
figure
set(gcf,'position',[200,200,400,300])
hold on
box on
[N,edges]=histcounts(signal,50);
for i=1:size(N,2)
    fill([(edges(i)),(edges(i+1)),(edges(i+1)),(edges(i))],[1,1,N(i),N(i)],'b')
end
set(gca,'yscale','log')
%set(gca,'xscale','log')
%ylim=get(gca,'ylim');
%set(gca,'ylim',[1,ylim(2)])
%set(gca,'xlim',[10^-2,10^6])
xlabel('信号强度(dBm)')
ylabel('数量')
hold off
